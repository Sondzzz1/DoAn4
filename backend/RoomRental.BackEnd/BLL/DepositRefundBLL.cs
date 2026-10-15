using Microsoft.EntityFrameworkCore;
using RoomRental.BackEnd.BLL.Interfaces;
using RoomRental.BackEnd.DAL;
using RoomRental.BackEnd.DTO.Payment;
using RoomRental.BackEnd.Models;
using RoomRental.BackEnd.Models.Enums;

namespace RoomRental.BackEnd.BLL;

public class DepositRefundBLL(ApplicationDbContext db, INotificationService notifications, ILogger<DepositRefundBLL> logger)
{
    public async Task<List<DepositRefundDto>> ListAsync(int accountId)
    {
        await PaymentSafetySchema.EnsureAsync(db);
        var user = await ActorAsync(accountId);
        var query = db.DepositRefundRequests.Include(r => r.Deposit).AsNoTracking();
        if (user.RoleId != 0) query = query.Where(r => r.Deposit.TenantAccountId == accountId || r.Deposit.LandlordAccountId == accountId);
        var rows = await query.OrderByDescending(r => r.RequestedAtUtc).ToListAsync();
        var result = new List<DepositRefundDto>();
        foreach (var row in rows) result.Add(await MapAsync(row));
        return result;
    }

    public async Task<DepositRefundDto> CreateAsync(int accountId, RefundCreateDto dto)
    {
        await PaymentSafetySchema.EnsureAsync(db);
        var reason = Required(dto.Reason, 5, 500, "Lý do hoàn cọc");
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        await WorkflowLock.AcquireAsync(db);
        var user = await ActorAsync(accountId);
        var deposit = await db.Deposits.FindAsync(dto.DepositId) ?? throw BusinessRuleException.NotFound("Không tìm thấy cọc.");
        if (user.RoleId != 1 || deposit.TenantAccountId != accountId) throw BusinessRuleException.Forbidden("Chỉ người thuê của khoản cọc được yêu cầu hoàn.");
        await EnsureBeforeContractAsync(deposit);
        if (deposit.Status is not (DepositStatus.Paid or DepositStatus.Confirmed) || deposit.Amount <= 0)
            throw BusinessRuleException.Conflict("Chỉ cọc đã thanh toán/xác nhận mới được đề nghị hoàn.");
        if (await db.PaymentCallbackEvents.AnyAsync(e => e.PaymentTransaction!.DepositId == deposit.Id && e.ProcessingState == PaymentEventState.RequiresReview))
            throw BusinessRuleException.Conflict("Cọc có giao dịch chưa đối soát; cần giải quyết trước khi hoàn.");
        if (await db.DepositRefundRequests.AnyAsync(r => r.DepositId == deposit.Id && r.Status <= DepositRefundStatus.RefundReported))
            throw BusinessRuleException.Conflict("Khoản cọc đã có yêu cầu hoàn đang xử lý.");
        var item = new DepositRefundRequest { Deposit = deposit, Amount = deposit.Amount, PreviousDepositStatus = deposit.Status,
            Reason = reason, RequestedByAccountId = accountId };
        deposit.Status = DepositStatus.RefundRequested;
        db.DepositRefundRequests.Add(item);
        await db.SaveChangesAsync(); await tx.CommitAsync();
        await NotifyAsync(item);
        return await MapAsync(item);
    }

    public async Task<DepositRefundDto> ActAsync(int accountId, int id, string action, RefundActionDto dto)
    {
        await PaymentSafetySchema.EnsureAsync(db);
        if (action == "confirm") await ContractClosurePolicy.EnsureSchemaAsync(db);
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        await WorkflowLock.AcquireAsync(db);
        var user = await ActorAsync(accountId);
        var item = await db.DepositRefundRequests.Include(r => r.Deposit).FirstOrDefaultAsync(r => r.Id == id)
            ?? throw BusinessRuleException.NotFound("Không tìm thấy yêu cầu hoàn cọc.");
        var tenant = user.RoleId == 1 && item.Deposit.TenantAccountId == accountId;
        var landlord = user.RoleId == 2 && item.Deposit.LandlordAccountId == accountId;
        var admin = user.RoleId == 0;
        if ((action is "approve" or "reject" or "report") ? !landlord : action == "confirm" ? !tenant && !admin : action == "withdraw" ? !tenant : true)
            throw BusinessRuleException.Forbidden("Bạn không có quyền thực hiện thao tác này.");
        if (action == "confirm" && item.Status == DepositRefundStatus.Completed) return await MapAsync(item);
        CheckVersion(item.RowVersion, dto.RowVersion);
        await EnsureBeforeContractAsync(item.Deposit);
        if (item.Deposit.Status != DepositStatus.RefundRequested || item.Amount != item.Deposit.Amount)
            throw BusinessRuleException.Conflict("Thông tin cọc đã thay đổi, cần kiểm tra lại.");
        if (action is not ("reject" or "withdraw") && await db.PaymentCallbackEvents.AnyAsync(e =>
            e.PaymentTransaction!.DepositId == item.DepositId && e.ProcessingState == PaymentEventState.RequiresReview))
            throw BusinessRuleException.Conflict("Có giao dịch phát sinh cần đối soát trước khi tiếp tục hoàn cọc.");
        var now = DateTime.UtcNow;
        switch (action)
        {
            case "approve" when item.Status == DepositRefundStatus.Requested:
                item.Status = DepositRefundStatus.Approved; item.ReviewedByAccountId = accountId; item.ReviewedAtUtc = now;
                item.DecisionNote = Optional(dto.Note, 1000); break;
            case "reject" when item.Status == DepositRefundStatus.Requested:
                item.DecisionNote = Required(dto.Note, 5, 1000, "Lý do từ chối");
                item.Status = DepositRefundStatus.Rejected; item.ReviewedByAccountId = accountId; item.ReviewedAtUtc = now;
                item.Deposit.Status = item.PreviousDepositStatus; break;
            case "withdraw" when item.Status is DepositRefundStatus.Requested or DepositRefundStatus.Approved:
                item.Status = DepositRefundStatus.Cancelled; item.Deposit.Status = item.PreviousDepositStatus; break;
            case "report" when item.Status == DepositRefundStatus.Approved:
                item.RefundReference = Required(dto.Reference, 3, 200, "Chứng từ/mã hoàn tiền");
                item.RefundReportedByAccountId = accountId; item.RefundReportedAtUtc = now;
                item.Status = DepositRefundStatus.RefundReported; break;
            case "confirm" when item.Status == DepositRefundStatus.RefundReported:
                if (admin) item.DecisionNote = Required(dto.Note, 10, 1000, "Bằng chứng đối soát của Admin");
                item.Status = DepositRefundStatus.Completed; item.CompletedByAccountId = accountId; item.CompletedAtUtc = now;
                item.Deposit.Status = DepositStatus.Refunded;
                var request = await db.RentalRequests.Include(r => r.Post).ThenInclude(p => p.Room).SingleAsync(r => r.Id == item.Deposit.RentalRequestId);
                request.Status = RentalRequestStatus.Cancelled;
                request.CancellationReason = "Đã hoàn đủ tiền cọc và xác nhận nhận lại tiền.";
                await db.SaveChangesAsync();
                var roomId = request.Post.RoomId;
                var hasRight = await db.RentalRequests.AnyAsync(r => r.Post.RoomId == roomId && r.Status == RentalRequestStatus.Approved) ||
                    await db.RentalContracts.AnyAsync(c => c.Post!.RoomId == roomId && RentalContractStatus.EffectiveStatuses.Contains(c.Status)) ||
                    await db.Deposits.AnyAsync(d => d.Id != item.DepositId &&
                        (d.Status == DepositStatus.Paid || d.Status == DepositStatus.Confirmed || d.Status == DepositStatus.RefundRequested) &&
                        !db.ContractSettlements.Any(s => s.DepositId == d.Id && s.Status == SettlementState.Completed) &&
                        db.RentalRequests.Any(r => r.Id == d.RentalRequestId && r.Post.RoomId == roomId && r.Status != RentalRequestStatus.ConvertedToContract));
                if (!hasRight && request.Post.Room.Status == RoomStatus.Reserved && !await ContractClosurePolicy.BlockedRoomIds(db).ContainsAsync(roomId)) request.Post.Room.Status = RoomStatus.Available;
                break;
            default: throw BusinessRuleException.Conflict("Trạng thái yêu cầu hoàn cọc không cho phép thao tác này.");
        }
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { throw BusinessRuleException.Conflict("Yêu cầu vừa được cập nhật. Vui lòng tải lại."); }
        await tx.CommitAsync();
        await NotifyAsync(item);
        return await MapAsync(item);
    }

    private async Task EnsureBeforeContractAsync(Deposit deposit)
    {
        if (await db.RentalContracts.AnyAsync(c => c.RentalRequestId == deposit.RentalRequestId))
            throw BusinessRuleException.Conflict("Cọc đã gắn hợp đồng; cần xử lý tất toán/bàn giao theo hợp đồng, không dùng luồng hoàn trước hợp đồng.");
    }
    private async Task<User> ActorAsync(int id) => await db.Users.FirstOrDefaultAsync(u => u.Id == id && u.IsActive)
        ?? throw BusinessRuleException.Forbidden("Tài khoản không khả dụng.");
    internal static string Required(string? value, int min, int max, string label)
    {
        var text = value?.Trim() ?? "";
        if (text.Length < min || text.Length > max) throw new BusinessRuleException($"{label} phải từ {min} đến {max} ký tự.");
        return text;
    }
    private static string? Optional(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : Required(value, 1, max, "Ghi chú");
    internal static void CheckVersion(byte[] version, string? expected)
    {
        if (expected != null && expected != Convert.ToBase64String(version)) throw BusinessRuleException.Conflict("Dữ liệu đã thay đổi, vui lòng tải lại trước khi xử lý.");
    }
    private async Task<DepositRefundDto> MapAsync(DepositRefundRequest r)
    {
        var request = await db.RentalRequests.Include(q => q.Post).ThenInclude(p => p.Room).AsNoTracking().SingleAsync(q => q.Id == r.Deposit.RentalRequestId);
        return new() { Id = r.Id, DepositId = r.DepositId, RentalRequestId = request.Id, RoomName = request.Post.Room.RoomName,
            Amount = r.Amount, Status = r.Status, Reason = r.Reason, DecisionNote = r.DecisionNote, RefundReference = r.RefundReference,
            RequestedAtUtc = r.RequestedAtUtc, RefundReportedAtUtc = r.RefundReportedAtUtc, CompletedAtUtc = r.CompletedAtUtc, RowVersion = Convert.ToBase64String(r.RowVersion) };
    }
    private async Task NotifyAsync(DepositRefundRequest r)
    {
        try
        {
            await notifications.CreateNotificationAsync(r.Deposit.TenantAccountId, "Cập nhật hoàn cọc", $"Yêu cầu #{r.Id}: {r.Amount:N0} VNĐ.", 1, "/tenant/rentals");
            await notifications.CreateNotificationAsync(r.Deposit.LandlordAccountId, "Cập nhật hoàn cọc", $"Yêu cầu #{r.Id}: {r.Amount:N0} VNĐ.", 1, "/landlord/contracts?tab=requests&section=deposits");
        }
        catch (Exception e) { logger.LogWarning(e, "Không gửi được thông báo hoàn cọc {Id}", r.Id); }
    }
}
