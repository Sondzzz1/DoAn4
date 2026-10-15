using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RoomRental.BackEnd.BLL.Interfaces;
using RoomRental.BackEnd.DAL;
using RoomRental.BackEnd.DTO.Payment;
using RoomRental.BackEnd.Helpers;
using RoomRental.BackEnd.Models;
using RoomRental.BackEnd.Models.Enums;

namespace RoomRental.BackEnd.BLL;

public class PaymentBLL : IPaymentService
{
    private const string DepositTarget = "Deposit";
    private const string MonthlyBillTarget = "MonthlyBill";
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _config;
    private readonly INotificationService _notificationService;
    private readonly ILogger<PaymentBLL> _logger;

    public PaymentBLL(
        ApplicationDbContext context,
        IConfiguration config,
        INotificationService notificationService,
        ILogger<PaymentBLL> logger)
    {
        _context = context;
        _config = config;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<PaymentResponseDto> CreatePaymentUrlAsync(int userId, CreatePaymentRequestDto dto, string clientIp)
    {
        await PaymentSafetySchema.EnsureAsync(_context);
        var vnpaySettings = GetVnPaySettings();
        var hasDeposit = dto.DepositId is > 0;
        var hasBill = dto.MonthlyBillId is > 0;
        if (hasDeposit == hasBill)
            throw new BusinessRuleException("Phải chọn đúng một đối tượng thanh toán: khoản cọc hoặc hóa đơn tháng.");

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await WorkflowLock.AcquireAsync(_context);
        await ExpireAttemptsAsync();
        Deposit? deposit = null;
        MonthlyBill? bill = null;
        decimal amount;
        string targetType;
        string orderInfo;

        if (hasDeposit)
        {
            deposit = await _context.Deposits.FirstOrDefaultAsync(d => d.Id == dto.DepositId)
                ?? throw BusinessRuleException.NotFound("Không tìm thấy khoản đặt cọc.");
            if (deposit.TenantAccountId != userId)
                throw BusinessRuleException.Forbidden("Bạn không có quyền thanh toán khoản đặt cọc này.");
            if (deposit.Status != DepositStatus.Pending)
                throw BusinessRuleException.Conflict("Chỉ khoản cọc đang chờ thanh toán mới có thể thanh toán.");
            if (deposit.DueAt is not null && deposit.DueAt <= DateTime.Now)
                throw BusinessRuleException.Conflict("Khoản đặt cọc đã hết hạn thanh toán.");
            if (await _context.PaymentCallbackEvents.AnyAsync(e => e.PaymentTransaction!.DepositId == deposit.Id && e.ProcessingState == PaymentEventState.RequiresReview))
                throw BusinessRuleException.Conflict("Khoản cọc có giao dịch cần đối soát; chưa được tạo lượt thanh toán mới.");
            await ValidateTargetForSuccessAsync(new PaymentTransaction { TargetType = DepositTarget, Deposit = deposit, Amount = deposit.Amount });
            if (await _context.PaymentTransactions.AnyAsync(p => p.DepositId == deposit.Id &&
                (p.Status == PaymentTransactionStatus.Pending || p.Status == PaymentTransactionStatus.Succeeded)))
                throw BusinessRuleException.Conflict("Khoản cọc đã có giao dịch đang xử lý hoặc đã thành công.");

            amount = deposit.Amount;
            targetType = DepositTarget;
            orderInfo = dto.OrderInfo ?? $"Thanh toan tien coc ID {deposit.Id}";
        }
        else
        {
            bill = await _context.MonthlyBills.Include(b => b.Contract)
                .FirstOrDefaultAsync(b => b.Id == dto.MonthlyBillId)
                ?? throw BusinessRuleException.NotFound("Không tìm thấy hóa đơn tháng.");
            if (bill.Contract.TenantAccountId != userId)
                throw BusinessRuleException.Forbidden("Bạn không có quyền thanh toán hóa đơn này.");
            if (bill.Status is not (MonthlyBillStatus.Unpaid or MonthlyBillStatus.Overdue))
                throw BusinessRuleException.Conflict("Hóa đơn không ở trạng thái có thể thanh toán.");
            if (await _context.PaymentCallbackEvents.AnyAsync(e => e.PaymentTransaction!.MonthlyBillId == bill.Id && e.ProcessingState == PaymentEventState.RequiresReview))
                throw BusinessRuleException.Conflict("Hóa đơn có giao dịch cần đối soát; chưa được tạo lượt thanh toán mới.");
            if (await _context.PaymentTransactions.AnyAsync(p => p.MonthlyBillId == bill.Id &&
                (p.Status == PaymentTransactionStatus.Pending || p.Status == PaymentTransactionStatus.Succeeded)))
                throw BusinessRuleException.Conflict("Hóa đơn đã có giao dịch đang xử lý hoặc đã thanh toán.");

            amount = bill.TotalAmount;
            targetType = MonthlyBillTarget;
            orderInfo = dto.OrderInfo ?? $"Thanh toan hoa don thang {bill.Month}/{bill.Year} ID {bill.Id}";
            bill.Status = MonthlyBillStatus.PendingPayment;
            bill.UpdatedAt = DateTime.Now;
        }

        var orderId = $"{(targetType == DepositTarget ? "D" : "B")}_{(deposit?.Id ?? bill!.Id)}_{DateTime.UtcNow.Ticks}";
        _context.PaymentTransactions.Add(new PaymentTransaction
        {
            DepositId = deposit?.Id,
            MonthlyBillId = bill?.Id,
            TargetType = targetType,
            OrderId = orderId,
            Amount = amount,
            Status = PaymentTransactionStatus.Pending,
            OrderInfo = orderInfo
        });
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        var vnpay = new VnPayHelper();
        vnpay.AddRequestData("vnp_Version", "2.1.0");
        vnpay.AddRequestData("vnp_Command", "pay");
        vnpay.AddRequestData("vnp_TmnCode", vnpaySettings.TmnCode);
        vnpay.AddRequestData("vnp_Amount", ((long)(amount * 100)).ToString(CultureInfo.InvariantCulture));
        vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
        vnpay.AddRequestData("vnp_ExpireDate", DateTime.Now.AddMinutes(30).ToString("yyyyMMddHHmmss"));
        vnpay.AddRequestData("vnp_CurrCode", "VND");
        vnpay.AddRequestData("vnp_IpAddr", string.IsNullOrEmpty(clientIp) || clientIp == "::1" ? "127.0.0.1" : clientIp);
        vnpay.AddRequestData("vnp_Locale", "vn");
        vnpay.AddRequestData("vnp_OrderInfo", orderInfo);
        vnpay.AddRequestData("vnp_OrderType", "other");
        vnpay.AddRequestData("vnp_ReturnUrl", vnpaySettings.ReturnUrl);
        vnpay.AddRequestData("vnp_TxnRef", orderId);

        var paymentUrl = vnpay.CreateRequestUrl(
            vnpaySettings.BaseUrl,
            vnpaySettings.HashSecret);
        return new PaymentResponseDto { PaymentUrl = paymentUrl, OrderId = orderId, Amount = amount };
    }

    public async Task<PaymentResultDto> ProcessPaymentReturnAsync(IQueryCollection query, byte source = PaymentEventSource.Ipn)
    {
        if (query.Count == 0)
            return Failure("Thiếu dữ liệu phản hồi VNPay.", "", "", "");
        var vnpay = new VnPayHelper();
        foreach (var (key, value) in query)
            if (key.StartsWith("vnp_", StringComparison.Ordinal)) vnpay.AddResponseData(key, value.ToString());

        var orderId = vnpay.GetResponseData("vnp_TxnRef");
        var transactionCode = vnpay.GetResponseData("vnp_TransactionNo");
        var responseCode = vnpay.GetResponseData("vnp_ResponseCode");
        var transactionStatus = vnpay.GetResponseData("vnp_TransactionStatus");
        var hashSecret = GetVnPaySettings().HashSecret;
        if (!vnpay.ValidateSignature(query["vnp_SecureHash"].ToString(), hashSecret))
            return Failure("Chữ ký VNPay không hợp lệ.", orderId, transactionCode, responseCode, callbackOutcome: PaymentCallbackOutcome.InvalidSignature);

        if (source > PaymentEventSource.Return || query.Any(p => p.Key.StartsWith("vnp_", StringComparison.Ordinal) && p.Value.Count != 1) ||
            orderId.Length is < 1 or > 100 || transactionCode.Length > 100 || !IsProviderStatus(responseCode) || !IsProviderStatus(transactionStatus))
            return Failure("Dữ liệu phản hồi VNPay không hợp lệ.", orderId, transactionCode, responseCode);
        if (!string.Equals(vnpay.GetResponseData("vnp_TmnCode"), GetVnPaySettings().TmnCode, StringComparison.Ordinal))
            return Failure("Merchant VNPay không khớp.", orderId, transactionCode, responseCode, callbackOutcome: PaymentCallbackOutcome.InvalidSignature);
        if (!long.TryParse(vnpay.GetResponseData("vnp_Amount"), NumberStyles.None, CultureInfo.InvariantCulture, out var rawAmount) || rawAmount < 0 || rawAmount > 999999999999999999)
            return Failure("Số tiền VNPay không hợp lệ.", orderId, transactionCode, responseCode, callbackOutcome: PaymentCallbackOutcome.InvalidAmount);

        await PaymentSafetySchema.EnsureAsync(_context);
        await using var dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await WorkflowLock.AcquireAsync(_context);
        await ExpireAttemptsAsync();
        var payment = await PaymentQuery().FirstOrDefaultAsync(p => p.OrderId == orderId);
        var canonical = JsonSerializer.Serialize(new { source, fields = query.Where(p => p.Key.StartsWith("vnp_", StringComparison.Ordinal) && p.Key is not ("vnp_SecureHash" or "vnp_SecureHashType"))
            .OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => new[] { p.Key, p.Value.ToString() }).ToArray() });
        var deduplicationKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
        var existing = await _context.PaymentCallbackEvents.FirstOrDefaultAsync(e => e.DeduplicationKey == deduplicationKey);
        if (existing != null)
        {
            await dbTransaction.CommitAsync();
            return EventResult(existing, payment, true);
        }
        var storedTransactionCode = string.IsNullOrWhiteSpace(transactionCode) || transactionCode == "0" ? null : transactionCode;
        DateTime? paidAtUtc = DateTime.TryParseExact(vnpay.GetResponseData("vnp_PayDate"), "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var paidAt)
            ? new DateTimeOffset(paidAt, TimeSpan.FromHours(7)).UtcDateTime : null;
        var receipt = new PaymentCallbackEvent { PaymentTransactionId = payment?.Id, ProviderOrderId = orderId, ProviderTransactionCode = storedTransactionCode,
            DeduplicationKey = deduplicationKey, Source = source, ReportedAmount = rawAmount / 100m, ResponseCode = responseCode, ProviderStatus = transactionStatus,
            ProviderPaidAtUtc = paidAtUtc, ProcessingState = PaymentEventState.RequiresReview };
        _context.PaymentCallbackEvents.Add(receipt);
        var duplicate = false;
        var applied = false;
        var resolved = storedTransactionCode == null ? null : await _context.PaymentCallbackEvents.AsNoTracking().FirstOrDefaultAsync(e =>
            e.ProviderOrderId == orderId && e.ProviderTransactionCode == storedTransactionCode && e.ReportedAmount == receipt.ReportedAmount &&
            e.ResponseCode == responseCode && e.ProviderStatus == transactionStatus && e.ProcessingState == PaymentEventState.Resolved);
        if (resolved != null)
        {
            receipt.ProcessingState = PaymentEventState.Resolved; receipt.Reason = resolved.Reason;
            receipt.ResolvedByAccountId = resolved.ResolvedByAccountId; receipt.ResolvedAtUtc = resolved.ResolvedAtUtc; receipt.ResolutionNote = resolved.ResolutionNote;
            duplicate = true;
        }
        else if (payment == null) receipt.Reason = "Không tìm thấy order; cần đối soát, không cấp quyền thuê.";
        else if (receipt.ReportedAmount != payment.Amount) receipt.Reason = "Số tiền provider báo khác số tiền kỳ vọng; không sửa số tiền gốc.";
        else if ((responseCode == "00") != (transactionStatus == "00")) receipt.Reason = "Hai mã kết quả provider không nhất quán.";
        else if (payment.Status == PaymentTransactionStatus.Succeeded)
        {
            if (payment.TransactionCode == storedTransactionCode && responseCode == "00") { receipt.ProcessingState = PaymentEventState.Applied; duplicate = true; }
            else receipt.Reason = "Callback khác giao dịch thành công đã ghi nhận; giữ nguyên lịch sử.";
        }
        else if (payment.Status == PaymentTransactionStatus.Failed && responseCode != "00" &&
            payment.ResponseCode == responseCode && (payment.TransactionCode == storedTransactionCode ||
                (payment.TransactionCode == "0" && storedTransactionCode == null)))
        {
            receipt.ProcessingState = PaymentEventState.Applied; duplicate = true;
        }
        else if (payment.Status != PaymentTransactionStatus.Pending) receipt.Reason = "Callback đến khi lượt thanh toán đã kết thúc; chưa áp dụng tiền vào quyền thuê/hóa đơn.";
        else if (storedTransactionCode != null && await _context.PaymentTransactions.AnyAsync(p => p.Id != payment.Id && p.TransactionCode == storedTransactionCode))
            receipt.Reason = "Mã giao dịch provider đã thuộc order khác.";
        else if (responseCode != "00")
        {
            payment.Status = PaymentTransactionStatus.Failed; payment.ResponseCode = responseCode;
            payment.TransactionCode = storedTransactionCode; payment.ProcessedAt = DateTime.Now;
            RestoreBillAfterFailedPayment(payment); receipt.ProcessingState = PaymentEventState.Applied;
        }
        else if (storedTransactionCode == null) receipt.Reason = "Provider báo thành công nhưng thiếu mã giao dịch thực.";
        else
        {
            try { await ValidateTargetForSuccessAsync(payment); }
            catch (BusinessRuleException e) { receipt.Reason = e.Message; }
            if (receipt.Reason == null)
            {
                payment.Status = PaymentTransactionStatus.Succeeded; payment.TransactionCode = storedTransactionCode;
                payment.ResponseCode = responseCode; payment.ProcessedAt = DateTime.Now;
                if (payment.Deposit != null) { payment.Deposit.Status = DepositStatus.Paid; payment.Deposit.PaidAt = DateTime.Now; }
                else { payment.MonthlyBill!.Status = MonthlyBillStatus.Paid; payment.MonthlyBill.PaidAt = DateTime.Now; payment.MonthlyBill.PaymentMethod = "VNPay"; }
                receipt.ProcessingState = PaymentEventState.Applied; applied = true;
            }
        }
        await _context.SaveChangesAsync();
        await dbTransaction.CommitAsync();
        if (applied) await SendSuccessNotificationsAsync(payment!);
        if (receipt.ProcessingState == PaymentEventState.RequiresReview)
        {
            _logger.LogWarning("Payment event {Id} requires reconciliation for order {OrderId}", receipt.Id, receipt.ProviderOrderId);
            await NotifyReviewAsync(receipt, payment);
        }
        return EventResult(receipt, payment, duplicate);
    }

    public async Task ReconcilePendingPaymentsAsync(CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await WorkflowLock.AcquireAsync(_context, cancellationToken);
        await ExpireAttemptsAsync();
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task ExpireAttemptsAsync()
    {
        var cutoff = DateTime.Now.AddMinutes(-30);
        var expired = await PaymentQuery().Where(p => p.Status == PaymentTransactionStatus.Pending && p.CreatedAt <= cutoff).ToListAsync();
        foreach (var attempt in expired)
        {
            attempt.Status = PaymentTransactionStatus.Expired;
            attempt.ProcessedAt = DateTime.Now;
            RestoreBillAfterFailedPayment(attempt);
        }
        if (expired.Count > 0) await _context.SaveChangesAsync();
    }

    private IQueryable<PaymentTransaction> PaymentQuery() => _context.PaymentTransactions
        .Include(p => p.Deposit)
        .Include(p => p.MonthlyBill).ThenInclude(b => b!.Contract);

    private VnPaySettings GetVnPaySettings() => new(
        GetRequiredVnPaySetting("TmnCode"),
        GetRequiredVnPaySetting("HashSecret"),
        GetRequiredVnPaySetting("BaseUrl"),
        GetRequiredVnPaySetting("ReturnUrl"));

    private string GetRequiredVnPaySetting(string key)
    {
        var value = _config[$"VnPay:{key}"];
        if (string.IsNullOrWhiteSpace(value))
            throw new BusinessRuleException("Dịch vụ thanh toán chưa sẵn sàng.", StatusCodes.Status503ServiceUnavailable);

        return value;
    }

    private sealed record VnPaySettings(string TmnCode, string HashSecret, string BaseUrl, string ReturnUrl);

    private async Task ValidateTargetForSuccessAsync(PaymentTransaction payment)
    {
        if (payment.TargetType == DepositTarget && payment.Deposit != null)
        {
            if (payment.Deposit.Amount != payment.Amount)
                throw BusinessRuleException.Conflict("Số tiền khoản cọc đã thay đổi, giao dịch bị từ chối.");
            if (payment.Deposit.DueAt <= DateTime.Now)
                throw BusinessRuleException.Conflict("Khoản cọc đã quá hạn thanh toán.");
            if (payment.Deposit.Status != DepositStatus.Pending)
                throw BusinessRuleException.Conflict("Khoản cọc không còn ở trạng thái chờ thanh toán.");
            var request = await _context.RentalRequests.Include(r => r.Post).ThenInclude(p => p.Room).SingleAsync(r => r.Id == payment.Deposit.RentalRequestId);
            if (request.Status != RentalRequestStatus.Approved || request.Post.Room.Status != RoomStatus.Reserved ||
                await _context.RentalRequests.AnyAsync(r => r.Id != request.Id && r.Post.RoomId == request.Post.RoomId && r.Status == RentalRequestStatus.Approved) ||
                await _context.RentalContracts.AnyAsync(c => c.Post!.RoomId == request.Post.RoomId && RentalContractStatus.EffectiveStatuses.Contains(c.Status)))
                throw BusinessRuleException.Conflict("Quyền giữ chỗ đã thay đổi; không tự cấp lại quyền thuê từ callback.");
            return;
        }

        if (payment.TargetType == MonthlyBillTarget && payment.MonthlyBill != null)
        {
            if (payment.MonthlyBill.TotalAmount != payment.Amount)
                throw BusinessRuleException.Conflict("Số tiền hóa đơn đã thay đổi, giao dịch bị từ chối.");
            if (payment.MonthlyBill.Status != MonthlyBillStatus.PendingPayment)
                throw BusinessRuleException.Conflict("Hóa đơn không còn ở trạng thái chờ thanh toán.");
            return;
        }

        throw BusinessRuleException.Conflict("Đối tượng thanh toán không hợp lệ.");
    }

    private static void RestoreBillAfterFailedPayment(PaymentTransaction payment)
    {
        if (payment.MonthlyBill?.Status == MonthlyBillStatus.PendingPayment)
            payment.MonthlyBill.Status = payment.MonthlyBill.DueDate < DateTime.Now
                ? MonthlyBillStatus.Overdue
                : MonthlyBillStatus.Unpaid;
    }

    private async Task SendSuccessNotificationsAsync(PaymentTransaction payment)
    {
        try
        {
            if (payment.Deposit != null)
            {
                await _notificationService.CreateNotificationAsync(payment.Deposit.TenantAccountId, "Đặt cọc thành công",
                    $"Bạn đã thanh toán {payment.Amount:N0} VNĐ tiền đặt cọc.", 1, "/tenant/rentals");
                await _notificationService.CreateNotificationAsync(payment.Deposit.LandlordAccountId, "Có khoản cọc mới",
                    $"Khách thuê đã thanh toán {payment.Amount:N0} VNĐ. Vui lòng xác nhận khoản cọc.", 1, "/landlord/contracts?tab=requests");
            }
            else if (payment.MonthlyBill != null)
            {
                await _notificationService.CreateNotificationAsync(payment.MonthlyBill.Contract.TenantAccountId,
                    "Thanh toán hóa đơn thành công", $"Bạn đã thanh toán {payment.Amount:N0} VNĐ qua VNPay.", 1, "/tenant/rentals");
                await _notificationService.CreateNotificationAsync(payment.MonthlyBill.Contract.LandlordAccountId,
                    "Hóa đơn đã được thanh toán", $"Hóa đơn {payment.MonthlyBill.Month}/{payment.MonthlyBill.Year} đã được thanh toán.", 1, "/landlord/contracts");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Thanh toán {OrderId} thành công nhưng không gửi được thông báo", payment.OrderId);
        }
    }

    private static bool IsProviderStatus(string value) => value.Length == 2 && value.All(c => c is >= '0' and <= '9');

    private async Task NotifyReviewAsync(PaymentCallbackEvent receipt, PaymentTransaction? payment)
    {
        try
        {
            if (await _context.PaymentCallbackEvents.AnyAsync(e => e.Id != receipt.Id && e.ProviderOrderId == receipt.ProviderOrderId &&
                e.ProviderTransactionCode == receipt.ProviderTransactionCode && e.ReportedAmount == receipt.ReportedAmount && e.ProcessingState == PaymentEventState.RequiresReview)) return;
            var ids = await _context.Users.Where(u => u.RoleId == 0 && u.IsActive).Select(u => u.Id).ToListAsync();
            foreach (var id in ids) await _notificationService.CreateNotificationAsync(id, "Giao dịch cần đối soát", $"Order {receipt.ProviderOrderId} đã lưu sự kiện #{receipt.Id} cần kiểm tra.", 1, "/admin/payments");
            var tenant = payment?.Deposit?.TenantAccountId ?? payment?.MonthlyBill?.Contract.TenantAccountId;
            if (tenant.HasValue) await _notificationService.CreateNotificationAsync(tenant.Value, "Giao dịch cần đối soát", "Phản hồi thanh toán đã được lưu để kiểm tra. Không thanh toán lại trước khi có kết quả đối soát.", 1, "/tenant/rentals");
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Không gửi được thông báo đối soát cho sự kiện {Id}", receipt.Id); }
    }

    private static PaymentResultDto EventResult(PaymentCallbackEvent e, PaymentTransaction? payment, bool duplicate)
    {
        var outcome = duplicate ? PaymentCallbackOutcome.AlreadyRecorded : PaymentCallbackOutcome.Recorded;
        if (payment == null) outcome = PaymentCallbackOutcome.OrderNotFound;
        else if (e.ReportedAmount != payment.Amount) outcome = PaymentCallbackOutcome.InvalidAmount;
        else if ((e.ResponseCode == "00") != (e.ProviderStatus == "00") || (e.ResponseCode == "00" && e.ProviderTransactionCode == null)) outcome = PaymentCallbackOutcome.NotProcessed;
        var success = e.ProcessingState == PaymentEventState.Applied && e.ResponseCode == "00" && e.ProviderStatus == "00";
        var result = success && payment != null ? Success(payment, "Giao dịch đã được ghi nhận thành công.", outcome)
            : Failure(e.ProcessingState == PaymentEventState.RequiresReview
                ? "Đã lưu phản hồi VNPay để đối soát. " + e.Reason + " Chưa áp dụng tiền vào quyền thuê/hóa đơn."
                : e.ProcessingState == PaymentEventState.Resolved ? "Đã đối soát ngoài hệ thống. " + e.ResolutionNote
                : $"Giao dịch không thành công (mã {e.ResponseCode}) đã được ghi nhận.",
                e.ProviderOrderId, e.ProviderTransactionCode ?? "", e.ResponseCode, payment, outcome);
        result.Amount = e.ReportedAmount;
        result.RequiresReview = e.ProcessingState == PaymentEventState.RequiresReview;
        result.EventState = e.ProcessingState;
        result.PaymentEventId = e.Id;
        return result;
    }

    private static PaymentResultDto Success(PaymentTransaction payment, string message,
        PaymentCallbackOutcome callbackOutcome = PaymentCallbackOutcome.Recorded) => new()
    {
        CallbackOutcome = callbackOutcome,
        Success = true, Message = message, OrderId = payment.OrderId,
        TransactionId = payment.TransactionCode ?? string.Empty, Amount = payment.Amount,
        ResponseCode = payment.ResponseCode ?? "00", DepositId = payment.DepositId,
        MonthlyBillId = payment.MonthlyBillId, ProcessedAt = payment.ProcessedAt,
        TargetType = payment.TargetType
    };

    private static PaymentResultDto Failure(
        string message, string orderId, string transactionCode, string responseCode,
        PaymentTransaction? payment = null, PaymentCallbackOutcome callbackOutcome = PaymentCallbackOutcome.NotProcessed) => new()
    {
        CallbackOutcome = callbackOutcome,
        Success = false, Message = message, OrderId = orderId, TransactionId = transactionCode,
        Amount = payment?.Amount ?? 0, ResponseCode = responseCode,
        DepositId = payment?.DepositId, MonthlyBillId = payment?.MonthlyBillId,
        ProcessedAt = payment?.ProcessedAt, TargetType = payment?.TargetType ?? string.Empty
    };
}
