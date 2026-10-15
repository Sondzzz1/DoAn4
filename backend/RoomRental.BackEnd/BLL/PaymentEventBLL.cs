using Microsoft.EntityFrameworkCore;
using RoomRental.BackEnd.DAL;
using RoomRental.BackEnd.DTO.Payment;
using RoomRental.BackEnd.Models;

namespace RoomRental.BackEnd.BLL;

public class PaymentEventBLL(ApplicationDbContext db)
{
    public async Task<List<PaymentEventDto>> ListAsync(int accountId, int page = 1)
    {
        await PaymentSafetySchema.EnsureAsync(db);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == accountId && u.IsActive) ?? throw BusinessRuleException.Forbidden("Tài khoản không khả dụng.");
        var q = db.PaymentCallbackEvents.Include(e => e.PaymentTransaction).ThenInclude(p => p!.Deposit)
            .Include(e => e.PaymentTransaction).ThenInclude(p => p!.MonthlyBill).ThenInclude(b => b!.Contract).AsNoTracking();
        if (user.RoleId != 0) q = q.Where(e => e.PaymentTransaction != null &&
            ((e.PaymentTransaction.Deposit != null && (e.PaymentTransaction.Deposit.TenantAccountId == accountId || e.PaymentTransaction.Deposit.LandlordAccountId == accountId)) ||
             (e.PaymentTransaction.MonthlyBill != null && (e.PaymentTransaction.MonthlyBill.Contract.TenantAccountId == accountId || e.PaymentTransaction.MonthlyBill.Contract.LandlordAccountId == accountId))));
        if (page < 1 || page > 100000) throw new BusinessRuleException("Trang không hợp lệ.");
        var rows = await q.OrderByDescending(e => e.ReceivedAtUtc).ThenByDescending(e => e.Id).Skip((page - 1) * 30).Take(30).ToListAsync();
        return rows.Select(Map).ToList();
    }
    public async Task<PaymentEventDto> ResolveAsync(int accountId, long id, RefundActionDto dto)
    {
        await PaymentSafetySchema.EnsureAsync(db);
        if (!await db.Users.AnyAsync(u => u.Id == accountId && u.RoleId == 0 && u.IsActive)) throw BusinessRuleException.Forbidden("Chỉ Admin được ghi kết quả đối soát.");
        var note = DepositRefundBLL.Required(dto.Note, 10, 1000, "Kết quả/bằng chứng đối soát");
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        await WorkflowLock.AcquireAsync(db);
        var e = await db.PaymentCallbackEvents.Include(x => x.PaymentTransaction).FirstOrDefaultAsync(x => x.Id == id)
            ?? throw BusinessRuleException.NotFound("Không tìm thấy sự kiện.");
        DepositRefundBLL.CheckVersion(e.RowVersion, dto.RowVersion);
        if (e.ProcessingState != PaymentEventState.RequiresReview) throw BusinessRuleException.Conflict("Sự kiện không còn chờ đối soát.");
        var related = await db.PaymentCallbackEvents.Where(x => x.ProcessingState == PaymentEventState.RequiresReview &&
            x.ProviderOrderId == e.ProviderOrderId && x.ProviderTransactionCode == e.ProviderTransactionCode && x.ReportedAmount == e.ReportedAmount &&
            x.ResponseCode == e.ResponseCode && x.ProviderStatus == e.ProviderStatus).ToListAsync();
        foreach (var item in related) { item.ProcessingState = PaymentEventState.Resolved; item.ResolvedByAccountId = accountId; item.ResolvedAtUtc = DateTime.UtcNow; item.ResolutionNote = note; }
        await db.SaveChangesAsync(); await tx.CommitAsync();
        return Map(e);
    }
    private static PaymentEventDto Map(PaymentCallbackEvent e) => new() { Id=e.Id, OrderId=e.ProviderOrderId, TransactionCode=e.ProviderTransactionCode,
        ReportedAmount=e.ReportedAmount, ExpectedAmount=e.PaymentTransaction?.Amount, Source=e.Source, ResponseCode=e.ResponseCode, ProviderStatus=e.ProviderStatus,
        ProcessingState=e.ProcessingState, Reason=e.Reason, ReceivedAtUtc=e.ReceivedAtUtc, ProviderPaidAtUtc=e.ProviderPaidAtUtc,
        ResolvedAtUtc=e.ResolvedAtUtc, ResolutionNote=e.ResolutionNote, RowVersion=Convert.ToBase64String(e.RowVersion) };
}
