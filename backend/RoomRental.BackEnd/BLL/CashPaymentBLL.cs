using System.Data;
using Microsoft.EntityFrameworkCore;
using RoomRental.BackEnd.BLL.Interfaces;
using RoomRental.BackEnd.DAL;
using RoomRental.BackEnd.DTO.Payment;
using RoomRental.BackEnd.Models;
using RoomRental.BackEnd.Models.Enums;

namespace RoomRental.BackEnd.BLL;

public class CashPaymentBLL(ApplicationDbContext db, INotificationService notifications, ILogger<CashPaymentBLL> logger)
{
    public async Task<List<CashPaymentDto>> ListAsync(int actorId)
    {
        await CashPaymentSchema.EnsureAsync(db);
        var actor = await ActorAsync(actorId);
        var query = db.CashPaymentReceipts.AsNoTracking();
        if (actor.RoleId != 0)
            query = query.Where(r => r.TenantAccountId == actorId || r.LandlordAccountId == actorId);
        var rows = await query.OrderByDescending(r => r.SelectedAtUtc).ThenByDescending(r => r.Id).ToListAsync();
        var result = new List<CashPaymentDto>();
        foreach (var row in rows) result.Add(await MapAsync(row));
        return result;
    }

    public async Task<CashPaymentDto> SelectAsync(int tenantId, CashPaymentSelectDto dto)
    {
        await CashPaymentSchema.EnsureAsync(db);
        await RequireRoleAsync(tenantId, 1);
        ValidateTargetIds(dto.DepositId, dto.MonthlyBillId);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await WorkflowLock.AcquireAsync(db);
        var target = await LoadTargetAsync(dto.DepositId, dto.MonthlyBillId);
        if (target.TenantId != tenantId)
            throw BusinessRuleException.Forbidden("Bạn không có quyền chọn phương thức thanh toán cho khoản thu này.");
        await ValidateTargetAsync(target);
        await CashPaymentPolicy.EnsureNoOnlineConflictAsync(db, dto.DepositId, dto.MonthlyBillId);
        var existing = await ActiveReceiptAsync(dto.DepositId, dto.MonthlyBillId);
        if (existing != null)
        {
            EnsureReceiptMatches(existing, target);
            if (existing.Status == CashPaymentStatus.Received)
                throw BusinessRuleException.Conflict("Khoản thu đã được thanh toán tiền mặt.");
            await transaction.CommitAsync();
            return await MapAsync(existing);
        }
        var row = NewReceipt(target);
        db.CashPaymentReceipts.Add(row);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        await NotifyAsync(target.LandlordId, "Khách thuê chọn trả tiền mặt",
            $"{target.Title}: {target.Amount:N0} VNĐ. Chỉ xác nhận sau khi đã thực nhận đủ tiền.", "/landlord/contracts");
        return await MapAsync(row);
    }

    public async Task<CashPaymentDto> ConfirmAsync(int landlordId, int id, CashPaymentConfirmDto? dto = null)
    {
        await CashPaymentSchema.EnsureAsync(db);
        await RequireRoleAsync(landlordId, 2);
        var note = CleanNote(dto?.Note);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await WorkflowLock.AcquireAsync(db);
        var row = await db.CashPaymentReceipts.FirstOrDefaultAsync(r => r.Id == id)
            ?? throw BusinessRuleException.NotFound("Không tìm thấy lựa chọn thanh toán tiền mặt.");
        if (row.LandlordAccountId != landlordId)
            throw BusinessRuleException.Forbidden("Chỉ chủ trọ của khoản thu mới được xác nhận nhận tiền mặt.");
        if (row.Status == CashPaymentStatus.Received)
        {
            await transaction.CommitAsync();
            return await MapAsync(row);
        }
        if (row.Status != CashPaymentStatus.Selected)
            throw BusinessRuleException.Conflict("Lựa chọn tiền mặt đã bị hủy.");
        var target = await LoadTargetAsync(row.DepositId, row.MonthlyBillId);
        EnsureReceiptMatches(row, target);
        await ReceiveAsync(row, target, landlordId, note);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        await NotifyReceiptAsync(row, target);
        return await MapAsync(row);
    }

    // The existing landlord monthly-bill action can create and confirm a receipt in the same transaction.
    public async Task<CashPaymentDto> ConfirmMonthlyBillAsync(int landlordId, int billId, string? note = null, decimal? expectedAmount = null)
    {
        await CashPaymentSchema.EnsureAsync(db);
        await RequireRoleAsync(landlordId, 2);
        note = CleanNote(note);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await WorkflowLock.AcquireAsync(db);
        var target = await LoadTargetAsync(null, billId);
        if (target.LandlordId != landlordId)
            throw BusinessRuleException.Forbidden("Chỉ chủ trọ của hóa đơn mới được xác nhận nhận tiền mặt.");
        if (expectedAmount is <= 0)
            throw new BusinessRuleException("Số tiền xác nhận nhận tiền mặt phải lớn hơn 0.");
        if (expectedAmount.HasValue && expectedAmount.Value != target.Amount)
            throw BusinessRuleException.Conflict("Số tiền hóa đơn đã thay đổi. Vui lòng tải lại và kiểm tra số tiền thực nhận trước khi xác nhận.");
        var row = await ActiveReceiptAsync(null, billId);
        if (row?.Status == CashPaymentStatus.Received)
        {
            EnsureReceiptMatches(row, target);
            await transaction.CommitAsync();
            return await MapAsync(row);
        }
        var isNew = row == null;
        row ??= NewReceipt(target);
        EnsureReceiptMatches(row, target);
        await ReceiveAsync(row, target, landlordId, note);
        if (isNew) db.CashPaymentReceipts.Add(row);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        await NotifyReceiptAsync(row, target);
        return await MapAsync(row);
    }

    public async Task<CashPaymentDto> CancelAsync(int tenantId, int id)
    {
        await CashPaymentSchema.EnsureAsync(db);
        await RequireRoleAsync(tenantId, 1);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await WorkflowLock.AcquireAsync(db);
        var row = await db.CashPaymentReceipts.FirstOrDefaultAsync(r => r.Id == id)
            ?? throw BusinessRuleException.NotFound("Không tìm thấy lựa chọn thanh toán tiền mặt.");
        if (row.TenantAccountId != tenantId)
            throw BusinessRuleException.Forbidden("Bạn không có quyền hủy lựa chọn tiền mặt này.");
        if (row.Status == CashPaymentStatus.Received)
            throw BusinessRuleException.Conflict("Tiền mặt đã được xác nhận; không thể hủy lịch sử thu tiền.");
        if (row.Status == CashPaymentStatus.Selected)
        {
            row.Status = CashPaymentStatus.Cancelled;
            row.CancelledAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
        await transaction.CommitAsync();
        return await MapAsync(row);
    }

    private async Task ReceiveAsync(CashPaymentReceipt row, Target target, int landlordId, string? note)
    {
        if (target.LandlordId != landlordId)
            throw BusinessRuleException.Forbidden("Chủ trọ của khoản thu đã thay đổi; chưa thể xác nhận phiếu thu này.");
        await ValidateTargetAsync(target);
        await CashPaymentPolicy.EnsureNoOnlineConflictAsync(db, row.DepositId, row.MonthlyBillId);
        if (target.Bill != null)
        {
            await ContractClosurePolicy.BillChangingAsync(db, target.Bill.ContractId, landlordId);
            target.Bill.Status = MonthlyBillStatus.Paid;
            target.Bill.PaidAt = DateTime.Now;
            target.Bill.PaymentMethod = "Tiền mặt";
            target.Bill.UpdatedAt = DateTime.Now;
        }
        else
        {
            target.Deposit!.Status = DepositStatus.Confirmed;
            target.Deposit.PaidAt = DateTime.Now;
            target.Deposit.UpdatedAt = DateTime.Now;
        }
        row.Status = CashPaymentStatus.Received;
        row.ConfirmedAtUtc = DateTime.UtcNow;
        row.ConfirmedByAccountId = landlordId;
        row.ReceiptNumber = $"TM-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}";
        row.Note = note;
    }

    private async Task<User> ActorAsync(int id) => await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id && u.IsActive)
        ?? throw BusinessRuleException.Forbidden("Tài khoản không hợp lệ.");

    private async Task RequireRoleAsync(int id, int role)
    {
        if ((await ActorAsync(id)).RoleId != role)
            throw BusinessRuleException.Forbidden(role == 1 ? "Chỉ người thuê được chọn phương thức tiền mặt." : "Chỉ chủ trọ được xác nhận thu tiền mặt.");
    }

    private static void ValidateTargetIds(int? depositId, int? billId)
    {
        if (depositId is <= 0 || billId is <= 0 || depositId.HasValue == billId.HasValue)
            throw new BusinessRuleException("Phải chọn đúng một khoản cọc hoặc hóa đơn hợp lệ.");
    }

    private Task<CashPaymentReceipt?> ActiveReceiptAsync(int? depositId, int? billId) => db.CashPaymentReceipts.FirstOrDefaultAsync(r =>
        ((depositId.HasValue && r.DepositId == depositId) || (billId.HasValue && r.MonthlyBillId == billId)) &&
        (r.Status == CashPaymentStatus.Selected || r.Status == CashPaymentStatus.Received));

    private static CashPaymentReceipt NewReceipt(Target target) => new()
    {
        DepositId = target.Deposit?.Id, MonthlyBillId = target.Bill?.Id, Amount = target.Amount,
        TenantAccountId = target.TenantId, LandlordAccountId = target.LandlordId,
        Status = CashPaymentStatus.Selected, SelectedAtUtc = DateTime.UtcNow
    };

    private static void EnsureReceiptMatches(CashPaymentReceipt row, Target target)
    {
        if (row.Amount != target.Amount || row.TenantAccountId != target.TenantId || row.LandlordAccountId != target.LandlordId)
            throw BusinessRuleException.Conflict("Số tiền hoặc người tham gia khoản thu đã thay đổi. Hãy hủy lựa chọn cũ và chọn lại tiền mặt.");
    }

    private async Task<Target> LoadTargetAsync(int? depositId, int? billId, bool tracking = true)
    {
        ValidateTargetIds(depositId, billId);
        if (depositId.HasValue)
        {
            var deposits = tracking ? db.Deposits.AsQueryable() : db.Deposits.AsNoTracking();
            var deposit = await deposits.FirstOrDefaultAsync(d => d.Id == depositId)
                ?? throw BusinessRuleException.NotFound("Không tìm thấy khoản đặt cọc.");
            var requests = tracking ? db.RentalRequests.AsQueryable() : db.RentalRequests.AsNoTracking();
            var request = await requests.Include(r => r.Post).ThenInclude(p => p.Room).FirstOrDefaultAsync(r => r.Id == deposit.RentalRequestId)
                ?? throw BusinessRuleException.NotFound("Không tìm thấy yêu cầu thuê của khoản cọc.");
            return new Target(deposit, null, request, deposit.TenantAccountId, deposit.LandlordAccountId, deposit.Amount,
                $"Tiền cọc · {request.Post.Title}", deposit.DueAt);
        }
        var bills = tracking ? db.MonthlyBills.AsQueryable() : db.MonthlyBills.AsNoTracking();
        var bill = await bills.Include(b => b.Contract).ThenInclude(c => c.Post).ThenInclude(p => p!.Room).FirstOrDefaultAsync(b => b.Id == billId)
            ?? throw BusinessRuleException.NotFound("Không tìm thấy hóa đơn.");
        return new Target(null, bill, null, bill.Contract.TenantAccountId, bill.Contract.LandlordAccountId, bill.TotalAmount,
            $"Hóa đơn {bill.Month}/{bill.Year} · {bill.Contract.Post?.Room.RoomName ?? $"Hợp đồng #{bill.ContractId}"}", bill.DueDate);
    }

    private async Task ValidateTargetAsync(Target target)
    {
        if (target.Amount <= 0 || decimal.Round(target.Amount, 2) != target.Amount)
            throw BusinessRuleException.Conflict("Số tiền khoản thu không hợp lệ.");
        if (target.TenantId == target.LandlordId)
            throw BusinessRuleException.Conflict("Người thuê và chủ trọ của khoản thu không hợp lệ.");
        if (target.Deposit != null)
        {
            var deposit = target.Deposit;
            var request = target.Request!;
            if (deposit.Status != DepositStatus.Pending)
                throw BusinessRuleException.Conflict("Chỉ khoản cọc đang chờ thanh toán mới được thu tiền mặt.");
            if (deposit.DueAt <= DateTime.Now)
                throw BusinessRuleException.Conflict("Khoản cọc đã hết hạn thanh toán; không được tự khôi phục quyền giữ phòng.");
            if (request.TenantAccountId != target.TenantId || request.LandlordAccountId != target.LandlordId)
                throw BusinessRuleException.Conflict("Thông tin người tham gia khoản cọc không khớp yêu cầu thuê.");
            await ContractClosurePolicy.EnsureRoomCanBeGrantedAsync(db, request.Post.RoomId, request.Id);
            if (request.Status != RentalRequestStatus.Approved || request.Post.Room.Status != RoomStatus.Reserved ||
                await db.RentalRequests.AnyAsync(r => r.Id != request.Id && r.Post.RoomId == request.Post.RoomId && r.Status == RentalRequestStatus.Approved) ||
                await db.RentalContracts.AnyAsync(c => c.Post!.RoomId == request.Post.RoomId && RentalContractStatus.EffectiveStatuses.Contains(c.Status)))
                throw BusinessRuleException.Conflict("Quyền giữ chỗ đã thay đổi; chưa được nhận cọc cho yêu cầu này.");
        }
        else
        {
            if (target.Bill!.Status is not (MonthlyBillStatus.Unpaid or MonthlyBillStatus.Overdue))
                throw BusinessRuleException.Conflict("Hóa đơn đã thanh toán, đã hủy hoặc đang thanh toán trực tuyến.");
            await CashPaymentPolicy.EnsureBillCollectionAllowedAsync(db, target.Bill.ContractId);
        }
    }

    private async Task<CashPaymentDto> MapAsync(CashPaymentReceipt row)
    {
        var users = await db.Users.AsNoTracking().Where(u => u.Id == row.TenantAccountId || u.Id == row.LandlordAccountId)
            .Select(u => new { u.Id, u.FullName, u.Phone }).ToListAsync();
        var dto = new CashPaymentDto
        {
            Id = row.Id, DepositId = row.DepositId, MonthlyBillId = row.MonthlyBillId, Amount = row.Amount, Status = row.Status,
            TenantAccountId = row.TenantAccountId, LandlordAccountId = row.LandlordAccountId,
            TenantName = users.FirstOrDefault(u => u.Id == row.TenantAccountId)?.FullName ?? "",
            LandlordName = users.FirstOrDefault(u => u.Id == row.LandlordAccountId)?.FullName ?? "",
            LandlordPhone = users.FirstOrDefault(u => u.Id == row.LandlordAccountId)?.Phone,
            SelectedAtUtc = row.SelectedAtUtc, ConfirmedAtUtc = row.ConfirmedAtUtc, CancelledAtUtc = row.CancelledAtUtc,
            ConfirmedByAccountId = row.ConfirmedByAccountId, ReceiptNumber = row.ReceiptNumber, Note = row.Note,
            TargetTitle = row.DepositId.HasValue ? $"Tiền cọc #{row.DepositId}" : $"Hóa đơn #{row.MonthlyBillId}"
        };
        try
        {
            var target = await LoadTargetAsync(row.DepositId, row.MonthlyBillId, tracking: false);
            dto.TargetTitle = target.Title;
            dto.TargetDueAt = target.DueAt;
            if (row.Status == CashPaymentStatus.Selected)
            {
                EnsureReceiptMatches(row, target);
                await ValidateTargetAsync(target);
                await CashPaymentPolicy.EnsureNoOnlineConflictAsync(db, row.DepositId, row.MonthlyBillId);
                dto.CanConfirm = true;
            }
        }
        catch (BusinessRuleException e) { if (row.Status == CashPaymentStatus.Selected) dto.UnavailableReason = e.Message; }
        return dto;
    }

    private static string? CleanNote(string? value)
    {
        if (value?.Length > 1000) throw new BusinessRuleException("Ghi chú phiếu thu tối đa 1000 ký tự.");
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private Task NotifyReceiptAsync(CashPaymentReceipt row, Target target) => NotifyAsync(target.TenantId,
        "Đã xác nhận nhận tiền mặt", $"{target.Title}: {row.Amount:N0} VNĐ. Phiếu thu {row.ReceiptNumber}.", "/tenant/rentals");

    private async Task NotifyAsync(int accountId, string title, string content, string link)
    {
        try { await notifications.CreateNotificationAsync(accountId, title, content, 1, link); }
        catch (Exception e) { logger.LogWarning(e, "Không thể gửi thông báo thu tiền mặt đến tài khoản {AccountId}", accountId); }
    }

    private sealed record Target(Deposit? Deposit, MonthlyBill? Bill, RentalRequest? Request,
        int TenantId, int LandlordId, decimal Amount, string Title, DateTime? DueAt);
}
