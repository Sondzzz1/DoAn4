using Microsoft.EntityFrameworkCore;
using RoomRental.BackEnd.DAL;
using RoomRental.BackEnd.Helpers;
using RoomRental.BackEnd.Models;
using RoomRental.BackEnd.Models.Enums;
using System.Text.Json;

namespace RoomRental.BackEnd.BLL;

internal static class ContractClosurePolicy
{
    public static async Task EnsureSchemaAsync(ApplicationDbContext db)
    {
        if (!db.Database.IsRelational()) return;
        var count = await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.tables WHERE schema_id=SCHEMA_ID('dbo') AND name IN ('HoSoKetThucHopDong','YeuCauKetThucHopDong','PhuongAnTatToanHopDong','KhoanMucTatToanHopDong','LichSuKetThucHopDong','NguoiOCungHopDong','TinTimNguoiOGhep','YeuCauOGhep','SuKienThanhToan','YeuCauHoanCoc')").SingleAsync();
        if (count != 10) throw new BusinessRuleException("Cần cập nhật SQL 07, 08 và 09_ContractClosure.sql trước khi dùng vòng đời thuê phòng.", 503);
    }

    public static IQueryable<int> BlockedRoomIds(ApplicationDbContext db) => db.RentalContracts.Where(c =>
        (c.Status == RentalContractStatus.Expired || c.Status == RentalContractStatus.Terminated || c.Status == RentalContractStatus.Cancelled) &&
        !db.ContractClosures.Any(h => h.ContractId == c.Id && h.ReleasedAtUtc != null)).Select(c => c.Post!.RoomId);

    public static async Task EnsureRoomCanBeGrantedAsync(ApplicationDbContext db, int roomId, int? allowedRequestId = null, int? allowedContractId = null)
    {
        await EnsureSchemaAsync(db);
        if (await BlockedRoomIds(db).ContainsAsync(roomId) ||
            await db.RentalContracts.AnyAsync(c => c.Post!.RoomId == roomId && c.Id != allowedContractId && RentalContractStatus.EffectiveStatuses.Contains(c.Status)) ||
            await db.RentalRequests.AnyAsync(r => r.Post.RoomId == roomId && r.Id != allowedRequestId && r.Status == RentalRequestStatus.Approved))
            throw BusinessRuleException.Conflict("Phòng còn hồ sơ kết thúc chưa bàn giao/tất toán hoặc chưa xác minh lịch sử; chưa được cho thuê lại.");
    }

    public static DateTime NaturalEndUtc(RentalContract c) => DateTime.SpecifyKind(c.EndDate.Date.AddDays(1).Subtract(RentalCalendar.VietnamOffset), DateTimeKind.Utc);
    public static DateTime LastDay(DateTime endUtc) => RentalCalendar.UtcDateInVietnam(endUtc.AddTicks(-1));

    public static async Task<ContractClosure> GetOrCreateAsync(ApplicationDbContext db, RentalContract c)
    {
        var h = await db.ContractClosures.FirstOrDefaultAsync(x => x.ContractId == c.Id);
        if (h != null) return h;
        h = new ContractClosure { ContractId = c.Id, RoomId = c.Post!.RoomId, Contract = c };
        // Legacy terminal dates are not inferred from mutable UpdatedAt.
        if (c.Status == RentalContractStatus.Expired) { h.EndKind = 0; h.EffectiveEndAtUtc = NaturalEndUtc(c); h.LastRentalDate = c.EndDate.Date; }
        db.ContractClosures.Add(h);
        await db.SaveChangesAsync();
        return h;
    }

    public static async Task EnsureNoFinancialConflictAsync(ApplicationDbContext db, RentalContract c)
    {
        if (await db.PaymentTransactions.AnyAsync(p => p.Status == PaymentTransactionStatus.Pending &&
            ((p.Deposit != null && p.Deposit.RentalRequestId == c.RentalRequestId) || (p.MonthlyBill != null && p.MonthlyBill.ContractId == c.Id))) ||
            await db.PaymentCallbackEvents.AnyAsync(e => e.ProcessingState == PaymentEventState.RequiresReview && e.PaymentTransaction != null &&
            ((e.PaymentTransaction.Deposit != null && e.PaymentTransaction.Deposit.RentalRequestId == c.RentalRequestId) ||
            (e.PaymentTransaction.MonthlyBill != null && e.PaymentTransaction.MonthlyBill.ContractId == c.Id))))
            throw BusinessRuleException.Conflict("Có thanh toán đang xử lý hoặc sự kiện cần đối soát; chưa được tất toán/mở phòng.");
    }

    // Monetary changes invalidate consent before any funds have been reported; afterwards require mediation.
    public static async Task BillChangingAsync(ApplicationDbContext db, int contractId, int? actorId)
    {
        await EnsureSchemaAsync(db);
        var h = await db.ContractClosures.FirstOrDefaultAsync(x => x.ContractId == contractId);
        if (h == null) return;
        var plan = await db.ContractSettlements.FirstOrDefaultAsync(x => x.ClosureId == h.Id && x.Status != SettlementState.Superseded);
        if (h.ReleasedAtUtc != null || plan?.Status == SettlementState.Completed || plan?.RefundReportedAtUtc != null || plan?.ExtraReportedAtUtc != null)
            throw BusinessRuleException.Conflict("Công nợ đã tất toán hoặc đã báo chuyển tiền. Cần đối soát, không được sửa/thu lại hóa đơn.");
        if (plan != null)
        {
            var before = plan.Status; plan.Status = SettlementState.Superseded;
            Event(db, h.Id, "billing-invalidated", actorId, before.ToString(), "Superseded", "Công nợ thay đổi, cần phương án và đồng thuận mới.", JsonSerializer.Serialize(plan));
        }
        h.BillingRevision++; h.BillingFinalizedAtUtc = null; h.BillingFinalizedByAccountId = null; h.UpdatedAtUtc = DateTime.UtcNow;
    }

    public static void Event(ApplicationDbContext db, int closureId, string type, int? actor, string? from = null, string? to = null,
        string? reason = null, string? snapshot = null, string? key = null, DateTime? now = null) => db.ContractClosureEvents.Add(new()
        { ClosureId = closureId, EventType = type, ActorAccountId = actor, Source = actor.HasValue ? (byte)0 : (byte)1,
          FromState = from, ToState = to, Reason = reason, SnapshotJson = snapshot, IdempotencyKey = key ?? Guid.NewGuid().ToString("N"), OccurredAtUtc = now ?? DateTime.UtcNow });

    public static async Task<List<RentalContract>> ReconcileAsync(ApplicationDbContext db, TimeProvider clock, CancellationToken ct = default)
    {
        await EnsureSchemaAsync(db);
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        await WorkflowLock.AcquireAsync(db, ct);
        var now = clock.GetUtcNow().UtcDateTime; var today = RentalCalendar.Today(clock);
        var ended = new List<RentalContract>();
        var active = await db.RentalContracts.Include(c => c.Post).ThenInclude(p => p!.Room)
            .Where(c => c.Status == RentalContractStatus.Active || c.Status == RentalContractStatus.PendingStart).ToListAsync(ct);
        foreach (var c in active)
        {
            var h = await db.ContractClosures.FirstOrDefaultAsync(x => x.ContractId == c.Id, ct);
            var agreed = h == null ? null : await db.ContractEndRequests.FirstOrDefaultAsync(x => x.ClosureId == h.Id && x.Status == ContractEndState.Agreed, ct);
            var end = agreed?.AgreedEffectiveEndAtUtc;
            var early = end.HasValue && end.Value <= now && end.Value <= NaturalEndUtc(c);
            if (early || c.EndDate.Date < today)
            {
                h ??= await GetOrCreateAsync(db, c);
                var previous = c.Status;
                c.Status = early ? RentalContractStatus.Terminated : RentalContractStatus.Expired;
                ended.Add(c);
                h.EndKind = early ? (byte)1 : (byte)0; h.EffectiveEndAtUtc = early ? end : NaturalEndUtc(c); h.LastRentalDate = LastDay(h.EffectiveEndAtUtc!.Value);
                h.UpdatedAtUtc = now;
                foreach (var request in await db.ContractEndRequests.Where(x => x.ClosureId == h.Id && (x.Status == 0 || x.Status == 1)).ToListAsync(ct))
                {
                    request.Status = early && request.Id == agreed!.Id ? ContractEndState.Effective : ContractEndState.Superseded;
                    if (request.Status == ContractEndState.Effective) request.EffectiveAtUtc = now;
                }
                await ContractOccupancyPolicy.EndAsync(db, c.Id, !early);
                Event(db, h.Id, "contract-ended", null, previous.ToString(), c.Status.ToString(), snapshot: JsonSerializer.Serialize(new { h.EffectiveEndAtUtc, h.LastRentalDate }), key: $"contract-ended:{c.Id}", now: now);
            }
            else if (c.Status == RentalContractStatus.PendingStart && c.StartDate.Date <= today)
            {
                h ??= await GetOrCreateAsync(db, c);
                c.Status = RentalContractStatus.Active;
                if (h?.HandoverStatus != HandoverState.Confirmed) c.Post!.Room.Status = RoomStatus.Rented;
                Event(db, h!.Id, "contract-activated", null, "5", "1", key: $"contract-activated:{c.Id}", now: now);
            }
        }
        await db.SaveChangesAsync(ct);
        foreach (var id in active.Select(c => c.Post!.RoomId).Distinct()) await ContractOccupancyPolicy.RecalculateCurrentOccupantsAsync(db, id);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return ended;
    }
}
