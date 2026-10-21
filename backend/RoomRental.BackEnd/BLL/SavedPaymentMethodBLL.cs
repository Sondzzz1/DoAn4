using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using RoomRental.BackEnd.DAL;
using RoomRental.BackEnd.DTO.Payment;
using RoomRental.BackEnd.Models;
using RoomRental.BackEnd.Models.Enums;

namespace RoomRental.BackEnd.BLL;

public sealed record SavedPaymentToken(string Token, string MerchantCode, int MethodId, string TokenFingerprint);

public static class SavedPaymentMethodSchema
{
    public const string MissingSchemaMessage = "Chức năng thẻ đã lưu cần cập nhật SQL 12_SavedPaymentMethods.sql trước khi sử dụng.";

    public static async Task<bool> IsReadyAsync(ApplicationDbContext db)
    {
        if (!db.Database.IsRelational()) return true;
        return await db.Database.SqlQueryRaw<int>("SELECT CAST(CASE WHEN OBJECT_ID(N'dbo.PhuongThucThanhToanDaLuu',N'U') IS NOT NULL AND OBJECT_ID(N'dbo.LienKetTheVNPay',N'U') IS NOT NULL THEN 1 ELSE 0 END AS int) AS [Value]").SingleAsync() == 1;
    }

    public static async Task EnsureAsync(ApplicationDbContext db)
    {
        if (!await IsReadyAsync(db)) throw new BusinessRuleException(MissingSchemaMessage, StatusCodes.Status503ServiceUnavailable);
    }
}

public class SavedPaymentMethodBLL(
    ApplicationDbContext db,
    IDataProtectionProvider protection,
    VnPayTokenProtocol protocol,
    ILogger<SavedPaymentMethodBLL> logger)
{
    public async Task<SavedPaymentMethodsDto> ListAsync(int actorId)
    {
        await RequireTenantAsync(actorId);
        if (!await SavedPaymentMethodSchema.IsReadyAsync(db))
            return new() { Enabled = false, UnavailableReason = SavedPaymentMethodSchema.MissingSchemaMessage };
        return new()
        {
            Enabled = protocol.IsEnabled,
            UnavailableReason = protocol.UnavailableReason,
            Methods = await db.SavedPaymentMethods.AsNoTracking().Where(m => m.UserId == actorId && m.IsActive)
                .OrderByDescending(m => m.CreatedAtUtc).Select(m => new SavedPaymentMethodDto
                {
                    Id = m.Id, BankCode = m.BankCode, MaskedCardNumber = m.MaskedCardNumber,
                    CardType = m.CardType, CreatedAtUtc = m.CreatedAtUtc
                }).ToListAsync()
        };
    }

    public async Task<VnPayCardLinkResponseDto> CreateLinkAsync(int actorId, VnPayCardLinkRequestDto dto, string clientIp)
    {
        protocol.EnsureEnabled();
        await SavedPaymentMethodSchema.EnsureAsync(db);
        if (dto.CardType is not ("01" or "02")) throw new BusinessRuleException("Loại thẻ liên kết không hợp lệ.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await WorkflowLock.AcquireAsync(db);
        await RequireTenantAsync(actorId);
        if (await db.SavedPaymentMethods.CountAsync(m => m.UserId == actorId && m.IsActive) >= 10)
            throw BusinessRuleException.Conflict("Bạn đã lưu tối đa 10 thẻ. Hãy gỡ thẻ không dùng trước khi liên kết thêm.");
        var now = DateTime.UtcNow;
        if (await db.VnPayCardLinks.CountAsync(l => l.UserId == actorId && l.Status == VnPayCardLinkStatus.Pending && l.ExpiresAtUtc > now) >= 3)
            throw new BusinessRuleException("Bạn đang có yêu cầu liên kết thẻ chưa hoàn tất. Vui lòng chờ ít phút trước khi thử lại.", StatusCodes.Status429TooManyRequests);
        var reference = "LINK" + Guid.NewGuid().ToString("N");
        var redirectUrl = protocol.CreateLinkUrl(reference, actorId, dto.CardType, clientIp);
        db.VnPayCardLinks.Add(new()
        {
            UserId = actorId, Reference = reference, MerchantCode = protocol.MerchantCode, CardType = dto.CardType,
            CreatedAtUtc = now, ExpiresAtUtc = now.AddMinutes(15)
        });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return new() { RedirectUrl = redirectUrl, Reference = reference };
    }

    // Only authenticated provider fields can create a stored method. This endpoint never applies money to a rental.
    public async Task<bool> CompleteLinkAsync(IQueryCollection query)
    {
        var fields = protocol.ValidateCallback(query);
        var reference = Required(fields, "vnp_txn_ref", 1, 100);
        var merchant = Required(fields, "vnp_tmn_code", 8, 8);
        var userValue = Required(fields, "vnp_app_user_id", 1, 255);
        if (Required(fields, "vnp_command", 1, 16) != "token_create" ||
            !int.TryParse(userValue, NumberStyles.None, CultureInfo.InvariantCulture, out var userId) || userId <= 0)
            throw new BusinessRuleException("Phản hồi liên kết thẻ không hợp lệ.");
        var response = Required(fields, "vnp_response_code", 2, 2);
        var status = Required(fields, "vnp_transaction_status", 2, 2);
        if (!Regex.IsMatch(response, "^[0-9]{2}$") || !Regex.IsMatch(status, "^[0-9]{2}$"))
            throw new BusinessRuleException("Mã kết quả liên kết thẻ không hợp lệ.");
        // Browser and IPN callbacks may differ in optional timestamps/descriptions. Deduplicate the verified decision.
        var success = response == "00" && status == "00";
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        {
            reference, merchant, userId, response, status,
            token = success ? Fingerprint(merchant, fields.GetValueOrDefault("vnp_token") ?? "") : null,
            cardType = success ? fields.GetValueOrDefault("vnp_card_type") : null,
            bankCode = success ? fields.GetValueOrDefault("vnp_bank_code") : null,
            lastFour = success ? Regex.Match(fields.GetValueOrDefault("vnp_card_number") ?? "", "[0-9]{4}$").Value : null
        }))));
        await SavedPaymentMethodSchema.EnsureAsync(db);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await WorkflowLock.AcquireAsync(db);
        var link = await db.VnPayCardLinks.FirstOrDefaultAsync(l => l.Reference == reference)
            ?? throw BusinessRuleException.NotFound("Không tìm thấy yêu cầu liên kết thẻ.");
        if (link.UserId != userId || link.MerchantCode != merchant || merchant != protocol.MerchantCode)
            throw new BusinessRuleException("Phản hồi không khớp chủ sở hữu yêu cầu liên kết thẻ.");
        await RequireTenantAsync(userId);
        if (link.Status != VnPayCardLinkStatus.Pending)
        {
            if (link.CallbackFingerprint != fingerprint)
                throw BusinessRuleException.Conflict("Yêu cầu liên kết đã được xử lý với kết quả khác.");
            await transaction.CommitAsync();
            return link.Status == VnPayCardLinkStatus.Completed;
        }
        if (link.ExpiresAtUtc <= DateTime.UtcNow)
            throw BusinessRuleException.Conflict("Yêu cầu liên kết thẻ đã hết hạn. Vui lòng tạo yêu cầu mới.");
        if (response != "00" || status != "00")
        {
            link.CallbackFingerprint = fingerprint;
            link.CompletedAtUtc = DateTime.UtcNow;
            link.Status = VnPayCardLinkStatus.Failed;
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return false;
        }
        var token = Required(fields, "vnp_token", 64, 64);
        if (!Regex.IsMatch(token, "^[A-Za-z0-9]{64}$")) throw new BusinessRuleException("Mã liên kết thẻ VNPay không hợp lệ.");
        var cardType = Required(fields, "vnp_card_type", 2, 2);
        if (cardType != link.CardType) throw new BusinessRuleException("Loại thẻ trả về không khớp yêu cầu liên kết.");
        var bankCode = Required(fields, "vnp_bank_code", 3, 20);
        if (!Regex.IsMatch(bankCode, "^[A-Za-z0-9]{3,20}$")) throw new BusinessRuleException("Mã ngân hàng trả về không hợp lệ.");
        var returnedCard = Required(fields, "vnp_card_number", 4, 40);
        var lastFour = Regex.Match(returnedCard, "[0-9]{4}$");
        if (!lastFour.Success) throw new BusinessRuleException("Không nhận được thông tin nhận diện thẻ đã che số.");
        var tokenFingerprint = Fingerprint(merchant, token);
        var method = await db.SavedPaymentMethods.FirstOrDefaultAsync(m => m.MerchantCode == merchant && m.TokenFingerprint == tokenFingerprint);
        if (method != null && method.UserId != userId)
            throw BusinessRuleException.Conflict("Mã liên kết thẻ đã thuộc tài khoản khác.");
        if ((method == null || !method.IsActive) && await db.SavedPaymentMethods.CountAsync(m => m.UserId == userId && m.IsActive) >= 10)
            throw BusinessRuleException.Conflict("Bạn đã lưu tối đa 10 thẻ. Hãy gỡ thẻ không dùng trước khi liên kết thêm.");
        if (method == null)
        {
            method = new() { UserId = userId, MerchantCode = merchant, TokenFingerprint = tokenFingerprint };
            db.SavedPaymentMethods.Add(method);
        }
        method.TokenCiphertext = Protector(userId, merchant).Protect(token);
        method.IsActive = true;
        method.UnlinkedAtUtc = null;
        method.BankCode = bankCode;
        // Retain only the last four digits, even if the provider accidentally returns an unmasked PAN.
        method.MaskedCardNumber = "•••• " + lastFour.Value;
        method.CardType = cardType;
        link.CallbackFingerprint = fingerprint;
        link.CompletedAtUtc = DateTime.UtcNow;
        link.Status = VnPayCardLinkStatus.Completed;
        link.SavedPaymentMethod = method;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return true;
    }

    public async Task UnlinkAsync(int actorId, int id)
    {
        await SavedPaymentMethodSchema.EnsureAsync(db);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await WorkflowLock.AcquireAsync(db);
        await RequireTenantAsync(actorId);
        var method = await db.SavedPaymentMethods.FirstOrDefaultAsync(m => m.Id == id && m.UserId == actorId)
            ?? throw BusinessRuleException.NotFound("Không tìm thấy thẻ đã lưu của bạn.");
        if (!method.IsActive) { await transaction.CommitAsync(); return; }
        if (await db.PaymentTransactions.AnyAsync(p => p.SavedPaymentMethodId == id && p.Status == PaymentTransactionStatus.Pending))
            throw BusinessRuleException.Conflict("Thẻ đang có thanh toán VNPay chưa hoàn tất. Vui lòng chờ xử lý trước khi gỡ.");
        method.IsActive = false;
        method.TokenCiphertext = null;
        method.UnlinkedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task<SavedPaymentToken> GetTokenAsync(int actorId, int id)
    {
        protocol.EnsureEnabled();
        await SavedPaymentMethodSchema.EnsureAsync(db);
        await RequireTenantAsync(actorId);
        var method = await db.SavedPaymentMethods.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id && m.UserId == actorId && m.IsActive)
            ?? throw BusinessRuleException.NotFound("Không tìm thấy thẻ đã lưu của bạn.");
        if (method.MerchantCode != protocol.MerchantCode)
            throw BusinessRuleException.Conflict("Thẻ đã lưu không thuộc kết nối VNPay hiện tại. Vui lòng liên kết lại.");
        try
        {
            if (string.IsNullOrWhiteSpace(method.TokenCiphertext)) throw new CryptographicException();
            var token = Protector(actorId, method.MerchantCode).Unprotect(method.TokenCiphertext!);
            if (!Regex.IsMatch(token, "^[A-Za-z0-9]{64}$") || Fingerprint(method.MerchantCode, token) != method.TokenFingerprint)
                throw new CryptographicException();
            return new(token, method.MerchantCode, method.Id, method.TokenFingerprint);
        }
        catch (CryptographicException)
        {
            logger.LogWarning("Cannot decrypt saved VNPay method {MethodId} for account {AccountId}", id, actorId);
            throw new BusinessRuleException("Thẻ đã lưu hiện không thể sử dụng. Vui lòng gỡ và liên kết lại thẻ.", StatusCodes.Status503ServiceUnavailable);
        }
    }

    public async Task<bool> ValidateBindingAsync(int actorId, int id, string merchantCode, string token)
    {
        await SavedPaymentMethodSchema.EnsureAsync(db);
        var fingerprint = Fingerprint(merchantCode, token);
        return await db.SavedPaymentMethods.AsNoTracking().AnyAsync(m => m.Id == id && m.UserId == actorId &&
            m.MerchantCode == merchantCode && m.TokenFingerprint == fingerprint);
    }

    private async Task RequireTenantAsync(int actorId)
    {
        if (!await db.Users.AsNoTracking().AnyAsync(u => u.Id == actorId && u.IsActive && u.RoleId == 1))
            throw BusinessRuleException.Forbidden("Chỉ người thuê đang hoạt động được quản lý thẻ thanh toán của mình.");
    }

    private IDataProtector Protector(int actorId, string merchant) => protection.CreateProtector("RoomRental.VnPayTokens.v1")
        .CreateProtector(actorId.ToString(CultureInfo.InvariantCulture)).CreateProtector(merchant);
    private static string Fingerprint(string merchant, string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(merchant + ":" + token)));

    private static string Required(IReadOnlyDictionary<string, string> fields, string key, int min, int max)
    {
        if (!fields.TryGetValue(key, out var value) || value.Length < min || value.Length > max || string.IsNullOrWhiteSpace(value))
            throw new BusinessRuleException("Phản hồi liên kết thẻ thiếu hoặc sai thông tin bắt buộc.");
        return value;
    }
}
