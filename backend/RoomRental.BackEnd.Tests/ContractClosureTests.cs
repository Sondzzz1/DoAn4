using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RoomRental.BackEnd.BLL;
using RoomRental.BackEnd.DAL;
using RoomRental.BackEnd.DTO.Rental;
using RoomRental.BackEnd.DTO.Room;
using RoomRental.BackEnd.Models;
using RoomRental.BackEnd.Models.Enums;
using Xunit;

namespace RoomRental.BackEnd.Tests;

public class ContractClosureTests
{
    internal sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Now;
    }
    internal static ContractClosureBLL Service(ApplicationDbContext db, Clock clock) => new(db, null!, NullLogger<ContractClosureBLL>.Instance, clock);
    internal static async Task Seed(ApplicationDbContext db, Clock clock, bool occupied = true)
    {
        await RoommateTests.SeedAsync(db);
        var c = await db.RentalContracts.SingleAsync();
        c.StartDate = clock.Now.ToOffset(TimeSpan.FromHours(7)).Date.AddMonths(-1); c.EndDate = c.StartDate.AddMonths(2);
        c.CreatedAt = clock.Now.UtcDateTime.AddMonths(-1);
        if (occupied)
        {
            db.ContractOccupants.Add(new() { ContractId = c.Id, TenantAccountId = 3, JoinedAt = c.StartDate.AddDays(1) });
            (await db.Rooms.SingleAsync()).CurrentOccupants = 2;
        }
        else { c.Status = RentalContractStatus.PendingStart; c.StartDate = c.EndDate.AddDays(-5); (await db.Rooms.SingleAsync()).Status = RoomStatus.Reserved; }
        var deposit = new Deposit { RentalRequestId = 1, TenantAccountId = 2, LandlordAccountId = 1, Amount = 1000000, Status = DepositStatus.Confirmed, PaidAt = clock.Now.UtcDateTime.AddDays(-2) };
        db.Add(deposit); await db.SaveChangesAsync();
        db.PaymentTransactions.Add(new() { DepositId = deposit.Id, OrderId = "closure-paid", TargetType = "Deposit", Amount = deposit.Amount,
            Status = PaymentTransactionStatus.Succeeded, TransactionCode = "closure-original", ProcessedAt = deposit.PaidAt });
        await db.SaveChangesAsync();
    }
    internal static async Task<ContractClosureView> Run(ApplicationDbContext db, Clock clock, int actor, string op, Action<ClosureCommandDto>? configure = null)
    {
        var service = Service(db, clock); var view = await service.GetAsync(actor, 1);
        var dto = new ClosureCommandDto { IdempotencyKey = Guid.NewGuid().ToString(), RowVersion = view.Closure == null ? null : Convert.ToBase64String(view.Closure.RowVersion),
            TargetId = op.EndsWith("-end") ? view.EndRequests.FirstOrDefault(x => x.Status == 0)?.Id : view.Settlements.FirstOrDefault(x => x.Status != SettlementState.Superseded)?.Id };
        configure?.Invoke(dto);
        try { return await service.ExecuteAsync(actor, 1, op, dto); }
        catch { db.ChangeTracker.Clear(); throw; }
    }
    internal static async Task End(ApplicationDbContext db, Clock clock)
    {
        await Run(db, clock, 2, "request-end", x => { x.Note = "Hai bên thống nhất kết thúc QA"; x.EffectiveEndAt = clock.Now; });
        await Run(db, clock, 1, "agree-end");
    }
    internal static async Task Handover(ApplicationDbContext db, Clock clock, bool never = false)
    {
        await Run(db, clock, 1, "save-handover", x => { x.HandoverKind = never ? HandoverKind.NeverTaken : HandoverKind.Returned; x.Condition = "Biên bản QA đúng tình trạng";
            x.ActualHandoverAt = clock.Now.AddMinutes(-1); x.ClosingElectricity = 10; x.ClosingWater = 20; x.EvidenceUrls = ["https://example.test/handover.jpg"]; });
        await Run(db, clock, 1, "confirm-handover"); await Run(db, clock, 2, "confirm-handover");
    }
    internal static async Task Plan(ApplicationDbContext db, Clock clock, decimal billAmount = 200000, decimal damage = 0, bool never = false)
    {
        if (!never)
        {
            var h = await db.ContractClosures.SingleAsync();
            db.MonthlyBills.Add(new() { ContractId = 1, Month = h.LastRentalDate!.Value.Month, Year = h.LastRentalDate.Value.Year, TotalAmount = billAmount,
                RoomPrice = billAmount, NewElectricity = 10, NewWater = 20, Status = MonthlyBillStatus.Unpaid }); await db.SaveChangesAsync();
        }
        await Run(db, clock, 1, "finalize-bills");
        await Run(db, clock, 1, "create-settlement", x => { if (damage > 0) x.Damages = [new() { Amount = damage, Description = "Thiệt hại QA có chứng cứ", EvidenceUrls = ["https://example.test/damage.jpg"] }]; });
        await Run(db, clock, 1, "propose-settlement");
        await Run(db, clock, 1, "agree-settlement"); await Run(db, clock, 2, "agree-settlement");
    }
    private static async Task<(ApplicationDbContext db, Clock clock)> Fixture(bool occupied = true)
    { var db = PostBLLTests.CreateContext(); var clock = new Clock(); await Seed(db, clock, occupied); return (db, clock); }

    [Fact]
    public async Task Active_contract_no_handover_cannot_release_and_legacy_endpoint_cannot_bypass()
    {
        var (db, clock) = await Fixture(); await using var context = db;
        await Assert.ThrowsAsync<BusinessRuleException>(() => Run(db, clock, 1, "release"));
        await Assert.ThrowsAsync<BusinessRuleException>(() => RoommateTests.Contracts(db).ChamDutAsync(1, 1));
        Assert.Equal(RoomStatus.Rented, (await db.Rooms.SingleAsync()).Status); Assert.Equal(2, (await db.Rooms.SingleAsync()).CurrentOccupants);
    }
    [Theory]
    [InlineData(3)] [InlineData(6)] [InlineData(8)]
    public async Task Wrong_tenant_landlord_or_admin_cannot_request_or_confirm(int actor)
    {
        var (db, clock) = await Fixture(); await using var context = db;
        Assert.Equal(403, (await Assert.ThrowsAsync<BusinessRuleException>(() => Run(db, clock, actor, "request-end", x => { x.Note = "Không có quyền"; x.EffectiveEndAt = clock.Now; }))).StatusCode);
        Assert.Empty(await db.ContractEndRequests.ToListAsync());
    }
    [Fact]
    public async Task Termination_requires_other_party_reason_and_does_not_vacate_or_refund()
    {
        var (db, clock) = await Fixture(); await using var context = db;
        await Run(db, clock, 1, "request-end", x => { x.Note = "Kết thúc theo thỏa thuận"; x.EffectiveEndAt = clock.Now; });
        await Assert.ThrowsAsync<BusinessRuleException>(() => Run(db, clock, 1, "agree-end"));
        Assert.Equal(RentalContractStatus.Active, (await db.RentalContracts.SingleAsync()).Status);
        await Run(db, clock, 2, "agree-end");
        Assert.Equal(RentalContractStatus.Terminated, (await db.RentalContracts.SingleAsync()).Status);
        Assert.Equal(RoomStatus.Rented, (await db.Rooms.SingleAsync()).Status); Assert.Equal(0, (await db.ContractOccupants.SingleAsync()).Status);
        Assert.Equal(DepositStatus.Confirmed, (await db.Deposits.SingleAsync()).Status);
        Assert.Equal("Kết thúc theo thỏa thuận", (await db.ContractEndRequests.SingleAsync()).Reason);
        Assert.NotEmpty(await db.ContractClosureEvents.ToListAsync());
    }
    [Theory]
    [InlineData("reject-end")] [InlineData("withdraw-end")]
    public async Task Rejected_or_withdrawn_proposal_does_not_terminate(string op)
    {
        var (db, clock) = await Fixture(); await using var context = db;
        await Run(db, clock, 2, "request-end", x => { x.Note = "Đề nghị kết thúc sớm"; x.EffectiveEndAt = clock.Now.AddDays(1); });
        await Run(db, clock, op == "reject-end" ? 1 : 2, op, x => x.Note = "Chưa đồng ý kết thúc");
        Assert.Equal(RentalContractStatus.Active, (await db.RentalContracts.SingleAsync()).Status);
    }
    [Fact]
    public async Task Future_agreement_and_worker_retries_end_once_and_keep_possession()
    {
        var (db, clock) = await Fixture(); await using var context = db;
        await Run(db, clock, 2, "request-end", x => { x.Note = "Hẹn ngày kết thúc"; x.EffectiveEndAt = clock.Now.AddHours(1); });
        await Run(db, clock, 1, "agree-end");
        var lifecycle = new RentalContractBLL(db, null!, NullLogger<RentalContractBLL>.Instance, clock);
        await lifecycle.ReconcileContractLifecycleAsync(72); Assert.Equal(1, (await db.RentalContracts.SingleAsync()).Status);
        clock.Now = clock.Now.AddHours(2); await lifecycle.ReconcileContractLifecycleAsync(72); await lifecycle.ReconcileContractLifecycleAsync(72);
        Assert.Equal(2, (await db.RentalContracts.SingleAsync()).Status);
        Assert.Single(await db.ContractClosureEvents.Where(x => x.EventType == "contract-ended").ToListAsync());
        Assert.Equal(RoomStatus.Rented, (await db.Rooms.SingleAsync()).Status); Assert.Equal(2, (await db.Rooms.SingleAsync()).CurrentOccupants);
    }
    [Fact]
    public async Task Natural_expiry_retains_unpaid_deposit_residents_and_is_read_only_on_get()
    {
        var (db, clock) = await Fixture(); await using var context = db;
        var c = await db.RentalContracts.SingleAsync(); c.EndDate = clock.Now.ToOffset(TimeSpan.FromHours(7)).Date.AddDays(-1); await db.SaveChangesAsync();
        db.MonthlyBills.Add(new() { ContractId = c.Id, Month = c.EndDate.Month, Year = c.EndDate.Year, TotalAmount = 200000, Status = MonthlyBillStatus.Unpaid, DueDate = c.EndDate });
        await db.SaveChangesAsync();
        var lifecycle = new RentalContractBLL(db, null!, NullLogger<RentalContractBLL>.Instance, clock);
        await lifecycle.ReconcileContractLifecycleAsync(72); await RoommateTests.Service(db).ReconcileLifecycleAsync();
        Assert.Equal(3, c.Status); Assert.Equal(2, (await db.Rooms.SingleAsync()).CurrentOccupants); Assert.Equal(0, (await db.ContractOccupants.SingleAsync()).Status);
        Assert.Equal(RoomStatus.Rented, (await db.Rooms.SingleAsync()).Status);
        Assert.Equal(MonthlyBillStatus.Unpaid, (await db.MonthlyBills.SingleAsync()).Status);
        var before = await ContractReadSafetyTests.SnapshotAsync(db); await Service(db, clock).GetAsync(2, 1); Assert.Equal(before, await ContractReadSafetyTests.SnapshotAsync(db));
    }
    [Fact]
    public async Task Naturally_expired_contract_handover_does_not_release_until_final_bill_and_deposit_settlement()
    {
        var (db, clock) = await Fixture(); await using var context = db;
        var c = await db.RentalContracts.SingleAsync(); c.EndDate = clock.Now.ToOffset(TimeSpan.FromHours(7)).Date.AddDays(-1); await db.SaveChangesAsync();
        await new RentalContractBLL(db, null!, NullLogger<RentalContractBLL>.Instance, clock).ReconcileContractLifecycleAsync(72);
        Assert.Equal(RentalContractStatus.Expired, c.Status);
        await Handover(db, clock);
        Assert.Equal(0, (await db.Rooms.SingleAsync()).CurrentOccupants); Assert.Equal(RoomStatus.Rented, (await db.Rooms.SingleAsync()).Status);
        await Assert.ThrowsAsync<BusinessRuleException>(() => Run(db, clock, 1, "release"));
        await Plan(db, clock);
        await Run(db, clock, 1, "report-refund", x => x.Reference = "QA-NATURAL-EXPIRY-REFUND");
        await Run(db, clock, 2, "confirm-refund"); await Run(db, clock, 2, "complete-settlement"); await Run(db, clock, 1, "release");
        Assert.Equal(RoomStatus.Available, (await db.Rooms.SingleAsync()).Status); Assert.Equal(RentalContractStatus.Expired, (await db.RentalContracts.SingleAsync()).Status);
    }
    [Theory]
    [InlineData(0, 0)] [InlineData(200000, 100000)] [InlineData(1000000, 0)] [InlineData(1200000, 300000)]
    public async Task Full_partial_zero_refund_and_extra_payment_preserve_history_and_release_only_at_end(decimal bill, decimal damage)
    {
        var never = bill == 0; var (db, clock) = await Fixture(!never); await using var context = db;
        var paidAt = (await db.Deposits.SingleAsync()).PaidAt;
        await End(db, clock); await Handover(db, clock, never); await Plan(db, clock, bill, damage, never);
        await Assert.ThrowsAsync<BusinessRuleException>(() => Run(db, clock, 1, "release"));
        var p = await db.ContractSettlements.SingleAsync();
        if (p.RefundDue > 0) { await Run(db, clock, 1, "report-refund", x => x.Reference = "QA-REFUND-RECEIPT"); await Assert.ThrowsAsync<BusinessRuleException>(() => Run(db, clock, 1, "confirm-refund")); await Run(db, clock, 2, "confirm-refund"); }
        if (p.ExtraDue > 0) { await Run(db, clock, 2, "report-extra", x => x.Reference = "QA-EXTRA-RECEIPT"); await Run(db, clock, 1, "confirm-extra"); }
        await Run(db, clock, 2, "complete-settlement");
        Assert.Equal(never ? RoomStatus.Reserved : RoomStatus.Rented, (await db.Rooms.SingleAsync()).Status);
        await Run(db, clock, 1, "release");
        Assert.Equal(RoomStatus.Available, (await db.Rooms.SingleAsync()).Status); Assert.Equal(0, (await db.Rooms.SingleAsync()).CurrentOccupants);
        Assert.Equal(DepositStatus.Confirmed, (await db.Deposits.SingleAsync()).Status); Assert.Equal(paidAt, (await db.Deposits.SingleAsync()).PaidAt);
        Assert.Equal(PaymentTransactionStatus.Succeeded, (await db.PaymentTransactions.SingleAsync()).Status);
        Assert.All(await db.MonthlyBills.ToListAsync(), b => { Assert.Equal(1, b.Status); Assert.Equal("ContractSettlement", b.PaymentMethod); });
        await Assert.ThrowsAsync<BusinessRuleException>(() => new MonthlyBillBLL(db, null!, NullLogger<MonthlyBillBLL>.Instance).ThanhToanAsync(1, 1));
    }
    [Fact]
    public async Task Disputed_deduction_blocks_transfer_and_admin_cannot_force_consent()
    {
        var (db, clock) = await Fixture(); await using var context = db; await End(db, clock); await Handover(db, clock); await Plan(db, clock, damage: 100000);
        await Run(db, clock, 2, "dispute-settlement", x => x.Note = "Không đồng ý khoản thiệt hại");
        await Assert.ThrowsAsync<BusinessRuleException>(() => Run(db, clock, 1, "report-refund", x => x.Reference = "QA-REFUND"));
        await Assert.ThrowsAsync<BusinessRuleException>(() => Run(db, clock, 8, "agree-settlement"));
        await Run(db, clock, 8, "mediate", x => { x.Note = "Admin ghi nhận cần hai bên kiểm tra bằng chứng"; x.EvidenceUrls = ["https://example.test/proof.jpg"]; });
        Assert.Equal(SettlementState.Disputed, (await db.ContractSettlements.SingleAsync()).Status);
    }
    [Fact]
    public async Task Handover_revision_invalidates_confirmations_and_bad_meters_or_base64_are_rejected()
    {
        var (db, clock) = await Fixture(); await using var context = db; await End(db, clock);
        await Assert.ThrowsAsync<BusinessRuleException>(() => Run(db, clock, 1, "save-handover", x => { x.HandoverKind = 0; x.Condition = "Tình trạng phòng QA"; x.ActualHandoverAt = clock.Now; x.ClosingElectricity = -1; x.ClosingWater = 0; }));
        await Handover(db, clock);
        await Assert.ThrowsAsync<BusinessRuleException>(() => Run(db, clock, 1, "save-handover", x => { x.HandoverKind = 0; x.Condition = "Tình trạng phòng QA"; x.ActualHandoverAt = clock.Now; x.ClosingElectricity = 10; x.ClosingWater = 20; x.EvidenceUrls = ["data:image/jpeg;base64,AAAA"]; }));
        db.ChangeTracker.Clear();
        var view = await Run(db, clock, 2, "save-handover", x => { x.HandoverKind = 0; x.Condition = "Cập nhật hiện trạng QA"; x.ActualHandoverAt = clock.Now; x.ClosingElectricity = 10; x.ClosingWater = 20; });
        Assert.Equal(2, view.Closure!.HandoverRevision); Assert.Null(view.Closure.TenantHandoverConfirmedAtUtc); Assert.Null(view.Closure.LandlordHandoverConfirmedAtUtc);
        Assert.Equal(HandoverState.PendingConfirmation, view.Closure.HandoverStatus);
    }
    [Fact]
    public async Task Bill_change_invalidates_plan_before_transfer_but_is_blocked_after_transfer()
    {
        var (db, clock) = await Fixture(); await using var context = db; await End(db, clock); await Handover(db, clock); await Plan(db, clock);
        var bill = await db.MonthlyBills.SingleAsync(); var bills = new MonthlyBillBLL(db, null!, NullLogger<MonthlyBillBLL>.Instance);
        await bills.CapNhatAsync(1, bill.Id, new() { SoDienMoi = 10, SoNuocMoi = 20, ChiPhiKhac = 10000 });
        Assert.Equal(SettlementState.Superseded, (await db.ContractSettlements.SingleAsync()).Status); Assert.Null((await db.ContractClosures.SingleAsync()).BillingFinalizedAtUtc);
        await Run(db, clock, 1, "finalize-bills"); await Run(db, clock, 1, "create-settlement"); await Run(db, clock, 1, "propose-settlement"); await Run(db, clock, 1, "agree-settlement"); await Run(db, clock, 2, "agree-settlement");
        await Run(db, clock, 1, "report-refund", x => x.Reference = "QA-TRANSFER-REPORTED");
        await Assert.ThrowsAsync<BusinessRuleException>(() => bills.ThanhToanAsync(1, bill.Id));
    }
    [Fact]
    public async Task Legacy_terminal_available_room_is_hidden_and_cannot_be_granted_or_manually_opened()
    {
        var (db, clock) = await Fixture(); await using var context = db;
        (await db.RentalContracts.SingleAsync()).Status = 2; (await db.Rooms.SingleAsync()).Status = RoomStatus.Available; await db.SaveChangesAsync();
        Assert.True((await Service(db, clock).GetAsync(1, 1)).NeedsReview);
        Assert.Empty((await new PostBLL(db).SearchPageAsync(new())).Items);
        await Assert.ThrowsAsync<BusinessRuleException>(() => new RoomBLL(db).UpdateRoomStatusAsync(1, 1, RoomStatus.Available));
        await Assert.ThrowsAsync<BusinessRuleException>(() => Run(db, clock, 1, "release"));
    }
    [Fact]
    public async Task Same_command_retry_is_idempotent_and_key_cannot_be_reused_for_other_content()
    {
        var (db, clock) = await Fixture(); await using var context = db; var service = Service(db, clock);
        var dto = new ClosureCommandDto { IdempotencyKey = Guid.NewGuid().ToString(), Note = "Đề nghị kết thúc QA", EffectiveEndAt = clock.Now };
        await service.ExecuteAsync(2, 1, "request-end", dto); await service.ExecuteAsync(2, 1, "request-end", dto);
        Assert.Single(await db.ContractEndRequests.ToListAsync()); Assert.Single(await db.ContractClosureEvents.Where(x => x.EventType == "request-end").ToListAsync());
        dto.Note = "Nội dung khác với lần đầu"; await Assert.ThrowsAsync<BusinessRuleException>(() => service.ExecuteAsync(2, 1, "request-end", dto));
    }

    [Fact]
    public async Task Dispute_after_report_can_resume_only_by_both_parties_without_second_transfer()
    {
        var (db, clock) = await Fixture(); await using var context = db; await End(db, clock); await Handover(db, clock); await Plan(db, clock);
        await Run(db, clock, 1, "report-refund", x => x.Reference = "QA-ORIGINAL-REFUND");
        var reported = (await db.ContractSettlements.SingleAsync()).RefundReportedAtUtc;
        await Run(db, clock, 2, "dispute-settlement", x => x.Note = "Cần đối chiếu lại chứng từ nhận tiền");
        await Assert.ThrowsAsync<BusinessRuleException>(() => Run(db, clock, 1, "create-settlement"));
        await Assert.ThrowsAsync<BusinessRuleException>(() => Run(db, clock, 8, "resume-settlement"));
        await Run(db, clock, 1, "resume-settlement"); Assert.Equal(SettlementState.Disputed, (await db.ContractSettlements.SingleAsync()).Status);
        await Run(db, clock, 2, "resume-settlement");
        Assert.Equal(SettlementState.TransferReported, (await db.ContractSettlements.SingleAsync()).Status);
        await Assert.ThrowsAsync<BusinessRuleException>(() => Run(db, clock, 1, "report-refund", x => x.Reference = "QA-SECOND-TRANSFER"));
        await Run(db, clock, 2, "confirm-refund"); await Run(db, clock, 2, "complete-settlement"); await Run(db, clock, 1, "release");
        Assert.Equal(reported, (await db.ContractSettlements.SingleAsync()).RefundReportedAtUtc);
        Assert.Equal("QA-ORIGINAL-REFUND", (await db.ContractSettlements.SingleAsync()).RefundReference);
    }

    [Fact]
    public async Task Deposit_mismatch_cannot_silently_change_contract_financial_snapshot()
    {
        var (db, clock) = await Fixture(false); await using var context = db; await End(db, clock); await Handover(db, clock, true);
        await Run(db, clock, 1, "finalize-bills");
        (await db.Deposits.SingleAsync()).Amount = 500000; await db.SaveChangesAsync();
        await Assert.ThrowsAsync<BusinessRuleException>(() => Run(db, clock, 1, "create-settlement"));
        Assert.Empty(await db.ContractSettlements.ToListAsync()); Assert.Equal(1000000, (await db.RentalContracts.SingleAsync()).DepositAmount);
        Assert.Equal("closure-original", (await db.PaymentTransactions.SingleAsync()).TransactionCode);
    }

    [Fact]
    public async Task Settled_unsigned_contract_deposit_is_history_not_a_hold_for_manual_room_or_next_refund()
    {
        var (db, clock) = await Fixture(false); await using var context = db;
        var c = await db.RentalContracts.SingleAsync(); c.Status = RentalContractStatus.PendingSignature; c.TenantConfirmed = false;
        c.CreatedAt = clock.Now.UtcDateTime.AddHours(-100); await db.SaveChangesAsync();
        await new RentalContractBLL(db, null!, NullLogger<RentalContractBLL>.Instance, clock).ReconcileContractLifecycleAsync(72);
        Assert.Equal(RentalContractStatus.Cancelled, c.Status);
        await Handover(db, clock, true); await Plan(db, clock, never: true);
        await Run(db, clock, 1, "report-refund", x => x.Reference = "QA-UNSIGNED-REFUND"); await Run(db, clock, 2, "confirm-refund");
        await Run(db, clock, 2, "complete-settlement"); await Run(db, clock, 1, "release");
        Assert.Equal(RentalRequestStatus.Cancelled, (await db.RentalRequests.SingleAsync()).Status);
        Assert.Equal(DepositStatus.Confirmed, (await db.Deposits.SingleAsync()).Status);
        var rooms = new RoomBLL(db); await rooms.UpdateRoomStatusAsync(1, 1, RoomStatus.TemporarilyUnavailable); await rooms.UpdateRoomStatusAsync(1, 1, RoomStatus.Available);
        var requests = new RentalRequestBLL(db, null!, NullLogger<RentalRequestBLL>.Instance);
        var next = await requests.TaoAsync(4, new() { BaiDangId = 1 });
        await requests.CapNhatTrangThaiAsync(1, next.Id, RentalRequestStatus.Approved, soTienDatCoc: 1000000, hanThanhToanCoc: DateTime.Now.AddDays(1));
        var d = await db.Deposits.SingleAsync(x => x.RentalRequestId == next.Id); d.Status = DepositStatus.Confirmed; await db.SaveChangesAsync();
        var refunds = PaymentSafetyTests.Refunds(db);
        var r = await refunds.CreateAsync(4, new() { DepositId = d.Id, Reason = "QA khách mới đổi kế hoạch thuê" });
        await refunds.ActAsync(1, r.Id, "approve", new()); await refunds.ActAsync(1, r.Id, "report", new() { Reference = "QA-NEXT-REFUND" });
        await refunds.ActAsync(4, r.Id, "confirm", new());
        Assert.Equal(RoomStatus.Available, (await db.Rooms.SingleAsync()).Status);
    }

    [Theory]
    [InlineData(RentalContractStatus.Terminated)] [InlineData(RentalContractStatus.Cancelled)]
    public async Task Legacy_terminal_end_date_is_not_fabricated_and_requires_both_parties(int status)
    {
        var (db, clock) = await Fixture(false); await using var context = db;
        (await db.RentalContracts.SingleAsync()).Status = status; await db.SaveChangesAsync();
        var v = await Service(db, clock).GetAsync(1, 1); Assert.True(v.NeedsReview); Assert.Null(v.Closure);
        await Run(db, clock, 2, "request-end", x => { x.Note = "Xác minh lại mốc kết thúc lịch sử"; x.EffectiveEndAt = clock.Now.AddDays(-2); x.EvidenceUrls = ["https://example.test/legacy.jpg"]; });
        Assert.Null((await db.ContractClosures.SingleAsync()).EffectiveEndAtUtc);
        await Run(db, clock, 1, "agree-end");
        Assert.Equal(status, (await db.RentalContracts.SingleAsync()).Status);
        Assert.Equal(clock.Now.AddDays(-2).UtcDateTime, (await db.ContractClosures.SingleAsync()).EffectiveEndAtUtc);
    }

    [Fact]
    public async Task Verified_multiple_terminal_histories_can_release_together_without_opening_over_active_rights()
    {
        var (db, clock) = await Fixture(false); await using var context = db; await End(db, clock); await Handover(db, clock, true); await Plan(db, clock, never: true);
        await Run(db, clock, 1, "report-refund", x => x.Reference = "QA-ORIGINAL-REFUND"); await Run(db, clock, 2, "confirm-refund"); await Run(db, clock, 2, "complete-settlement");
        var c = new RentalContract { RentalRequestId = 2, PostId = 1, TenantAccountId = 2, LandlordAccountId = 1, Status = RentalContractStatus.Active, StartDate = DateTime.Today, EndDate = DateTime.Today.AddMonths(1) };
        db.Add(new RentalRequest { Id = 2, PostId = 1, TenantAccountId = 2, LandlordAccountId = 1, Status = RentalRequestStatus.ConvertedToContract }); db.Add(c); await db.SaveChangesAsync();
        await Assert.ThrowsAsync<BusinessRuleException>(() => Run(db, clock, 1, "release"));
        db.ChangeTracker.Clear(); c = await db.RentalContracts.SingleAsync(x => x.Id != 1); c.Status = RentalContractStatus.Terminated;
        var h = new ContractClosure { ContractId = c.Id, RoomId = 1, EndKind = 1, EffectiveEndAtUtc = clock.Now.UtcDateTime, LastRentalDate = DateTime.Today,
            HandoverKind = HandoverKind.NeverTaken, HandoverStatus = HandoverState.Confirmed, TenantHandoverConfirmedByAccountId = 2, TenantHandoverConfirmedAtUtc = clock.Now.UtcDateTime,
            LandlordHandoverConfirmedByAccountId = 1, LandlordHandoverConfirmedAtUtc = clock.Now.UtcDateTime, BillingFinalizedAtUtc = clock.Now.UtcDateTime, BillingFinalizedByAccountId = 1 };
        var d = new Deposit { RentalRequestId = 2, TenantAccountId = 2, LandlordAccountId = 1, Status = DepositStatus.Confirmed, Amount = 100 };
        db.AddRange(h, d); await db.SaveChangesAsync();
        db.Add(new ContractSettlement { ClosureId = h.Id, DepositId = d.Id, Revision = 1, Status = SettlementState.Completed, DepositAmountSnapshot = 100, RefundDue = 100,
            ProposedByAccountId = 1, TenantAgreedByAccountId = 2, TenantAgreedAtUtc = clock.Now.UtcDateTime, LandlordAgreedByAccountId = 1, LandlordAgreedAtUtc = clock.Now.UtcDateTime,
            RefundReference = "QA-HISTORY-RECEIPT", RefundReportedByAccountId = 1, RefundReportedAtUtc = clock.Now.UtcDateTime, RefundReceivedByAccountId = 2, RefundReceivedAtUtc = clock.Now.UtcDateTime,
            CompletedAtUtc = clock.Now.UtcDateTime, CompletedByAccountId = 2 }); await db.SaveChangesAsync();
        await Run(db, clock, 1, "release");
        Assert.All(await db.ContractClosures.ToListAsync(), x => Assert.NotNull(x.ReleasedAtUtc)); Assert.Equal(RoomStatus.Available, (await db.Rooms.SingleAsync()).Status);
    }
}
