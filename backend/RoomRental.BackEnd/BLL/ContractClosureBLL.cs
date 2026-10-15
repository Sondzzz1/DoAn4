using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoomRental.BackEnd.BLL.Interfaces;
using RoomRental.BackEnd.DAL;
using RoomRental.BackEnd.DTO.Rental;
using RoomRental.BackEnd.Models;
using RoomRental.BackEnd.Models.Enums;

namespace RoomRental.BackEnd.BLL;

public class ContractClosureBLL(ApplicationDbContext db, INotificationService notifications, ILogger<ContractClosureBLL> logger, TimeProvider? clock = null)
{
    private DateTime Now => (clock ?? TimeProvider.System).GetUtcNow().UtcDateTime;
    private async Task<User> Actor(int id) => await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id && u.IsActive)
        ?? throw BusinessRuleException.Forbidden("Tài khoản không hợp lệ.");
    private async Task<RentalContract> Contract(int id) => await db.RentalContracts.Include(c => c.Post).ThenInclude(p => p!.Room)
        .FirstOrDefaultAsync(c => c.Id == id) ?? throw BusinessRuleException.NotFound("Không tìm thấy hợp đồng.");
    private static void Participant(User u, RentalContract c)
    {
        if (!((u.RoleId == 1 && u.Id == c.TenantAccountId) || (u.RoleId == 2 && u.Id == c.LandlordAccountId)))
            throw BusinessRuleException.Forbidden("Bạn không phải hai bên của hợp đồng này.");
    }
    private static bool Terminal(RentalContract c) => c.Status is RentalContractStatus.Expired or RentalContractStatus.Terminated or RentalContractStatus.Cancelled;
    private static string Required(string? value, int max = 1000) => DepositRefundBLL.Required(value, 5, max, "Nội dung");
    private static string Evidence(List<string> urls, bool required = false)
    {
        if (urls.Count > 10 || (required && urls.Count == 0)) throw new BusinessRuleException("Cần chứng cứ URL hợp lệ, tối đa 10 ảnh/tài liệu.");
        var clean = urls.Select(s => s?.Trim() ?? "").ToList();
        if (clean.Any(s => s.Length is < 1 or > 2000 || !(s.StartsWith("/uploads/", StringComparison.Ordinal) && !s.Contains("..") && !s.Contains('\\') ||
            Uri.TryCreate(s, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" && string.IsNullOrEmpty(uri.UserInfo))))
            throw new BusinessRuleException("Chứng cứ chỉ nhận URL http/https hoặc /uploads/, không nhận Base64.");
        return JsonSerializer.Serialize(clean.Distinct(StringComparer.Ordinal));
    }
    private static void Version(ContractClosure h, string? expected)
    {
        if (h.RowVersion.Length > 0 && expected == null) throw BusinessRuleException.Conflict("Thiếu RowVersion. Vui lòng tải lại hồ sơ.");
        DepositRefundBLL.CheckVersion(h.RowVersion, expected);
    }

    public async Task<ContractClosureView> GetAsync(int actorId, int contractId)
    {
        await ContractClosurePolicy.EnsureSchemaAsync(db);
        var u = await Actor(actorId); var c = await Contract(contractId);
        if (u.RoleId != 0) Participant(u, c);
        var h = await db.ContractClosures.AsNoTracking().FirstOrDefaultAsync(x => x.ContractId == c.Id);
        var plans = h == null ? [] : await db.ContractSettlements.AsNoTracking().Include(x => x.Lines).Where(x => x.ClosureId == h.Id).OrderByDescending(x => x.Revision).ToListAsync();
        var ends = h == null ? [] : await db.ContractEndRequests.AsNoTracking().Where(x => x.ClosureId == h.Id).OrderByDescending(x => x.Id).ToListAsync();
        var bills = await db.MonthlyBills.AsNoTracking().Where(x => x.ContractId == c.Id).OrderBy(x => x.Year).ThenBy(x => x.Month)
            .Select(x => new ClosureBillView(x.Id, x.Month, x.Year, x.Status, x.TotalAmount, x.NewElectricity, x.NewWater)).ToListAsync();
        var blockers = await Blockers(c, h, plans.FirstOrDefault(x => x.Status != SettlementState.Superseded));
        var ops = new List<string>();
        if (u.RoleId == 0) ops.Add("mediate");
        else if (h?.ReleasedAtUtc == null)
        {
            if (c.Status is RentalContractStatus.Active or RentalContractStatus.PendingStart || (c.Status is RentalContractStatus.Terminated or RentalContractStatus.Cancelled) && h?.EffectiveEndAtUtc == null)
            {
                var pending = ends.FirstOrDefault(x => x.Status is ContractEndState.Requested or ContractEndState.Agreed);
                if (pending == null) ops.Add("request-end");
                else if (pending.Status == ContractEndState.Requested)
                {
                    if (pending.RequestedByAccountId == actorId) ops.Add("withdraw-end"); else ops.AddRange(["agree-end", "reject-end"]);
                }
            }
            var plan = plans.FirstOrDefault(x => x.Status != SettlementState.Superseded);
            var moneyLocked = plan?.Status == SettlementState.Completed || plan?.RefundReportedAtUtc != null || plan?.ExtraReportedAtUtc != null;
            if (c.Status != RentalContractStatus.PendingSignature && !moneyLocked)
                ops.Add("save-handover");
            if (h?.HandoverStatus == HandoverState.PendingConfirmation)
            {
                if (actorId == c.TenantAccountId ? h.TenantHandoverConfirmedAtUtc == null : h.LandlordHandoverConfirmedAtUtc == null) ops.Add("confirm-handover");
                ops.Add("dispute-handover");
            }
            if (actorId == c.LandlordAccountId && Terminal(c) && h?.HandoverStatus == HandoverState.Confirmed && !moneyLocked)
            {
                ops.Add("finalize-bills");
                if (h.BillingFinalizedAtUtc != null) ops.Add("create-settlement");
            }
            if (plan != null)
            {
                if (plan.Status == SettlementState.Draft && actorId == plan.ProposedByAccountId) ops.Add("propose-settlement");
                if (plan.Status == SettlementState.Proposed && (actorId == c.TenantAccountId ? plan.TenantAgreedAtUtc == null : plan.LandlordAgreedAtUtc == null)) ops.Add("agree-settlement");
                if (plan.Status is SettlementState.Proposed or SettlementState.Agreed or SettlementState.TransferReported) ops.Add("dispute-settlement");
                if (plan.Status == SettlementState.Disputed && (actorId == c.TenantAccountId ? plan.TenantAgreedAtUtc == null : plan.LandlordAgreedAtUtc == null)) ops.Add("resume-settlement");
                if (plan.Status is SettlementState.Agreed or SettlementState.TransferReported)
                {
                    if (plan.RefundDue > 0 && actorId == c.LandlordAccountId && plan.RefundReportedAtUtc == null) ops.Add("report-refund");
                    if (plan.RefundReportedAtUtc != null && actorId == c.TenantAccountId && plan.RefundReceivedAtUtc == null) ops.Add("confirm-refund");
                    if (plan.ExtraDue > 0 && actorId == c.TenantAccountId && plan.ExtraReportedAtUtc == null) ops.Add("report-extra");
                    if (plan.ExtraReportedAtUtc != null && actorId == c.LandlordAccountId && plan.ExtraReceivedAtUtc == null) ops.Add("confirm-extra");
                    if ((plan.RefundDue == 0 || plan.RefundReceivedAtUtc != null) && (plan.ExtraDue == 0 || plan.ExtraReceivedAtUtc != null)) ops.Add("complete-settlement");
                }
            }
            if (actorId == c.LandlordAccountId && blockers.Count == 0) ops.Add("release");
        }
        return new() { ContractId = c.Id, ContractStatus = c.Status, TenantAccountId = c.TenantAccountId, LandlordAccountId = c.LandlordAccountId,
            NeedsReview = Terminal(c) && (h == null || h.EffectiveEndAtUtc == null), Closure = h, EndRequests = ends, Settlements = plans,
            Events = h == null ? [] : await db.ContractClosureEvents.AsNoTracking().Where(x => x.ClosureId == h.Id).OrderByDescending(x => x.Id).ToListAsync(),
            Bills = bills, ReleaseBlockers = blockers, AllowedOperations = ops };
    }

    private async Task<List<string>> Blockers(RentalContract c, ContractClosure? h, ContractSettlement? plan)
    {
        var reasons = new List<string>();
        if (!Terminal(c)) reasons.Add("Hợp đồng chưa kết thúc hiệu lực.");
        if (h?.EffectiveEndAtUtc == null) reasons.Add("Chưa xác minh mốc kết thúc hợp đồng.");
        if (h?.HandoverStatus != HandoverState.Confirmed) reasons.Add("Hai bên chưa xác nhận bàn giao.");
        if (h?.BillingFinalizedAtUtc == null) reasons.Add("Chưa chốt hóa đơn cuối/công nợ.");
        if (plan?.Status != SettlementState.Completed) reasons.Add("Cọc/công nợ chưa được tất toán.");
        if (await db.MonthlyBills.AnyAsync(b => b.ContractId == c.Id && b.Status != MonthlyBillStatus.Paid && b.Status != MonthlyBillStatus.Cancelled)) reasons.Add("Còn hóa đơn chưa thanh toán.");
        if (await db.ContractOccupants.AnyAsync(o => o.ContractId == c.Id && o.Status == ContractOccupantStatus.Active)) reasons.Add("Còn người ở cùng chưa bàn giao.");
        var roomId = c.Post!.RoomId;
        var verifiedContracts = db.ContractClosures.Where(y => y.ReleasedAtUtc != null ||
            y.EffectiveEndAtUtc != null && y.HandoverStatus == HandoverState.Confirmed && y.BillingFinalizedAtUtc != null &&
            db.ContractSettlements.Any(s => s.ClosureId == y.Id && s.Status == SettlementState.Completed)).Select(y => y.ContractId);
        if (await db.RentalContracts.AnyAsync(x => x.Id != c.Id && x.Post!.RoomId == roomId &&
            (x.Status != RentalContractStatus.Terminated && x.Status != RentalContractStatus.Expired && x.Status != RentalContractStatus.Cancelled || !verifiedContracts.Contains(x.Id))))
            reasons.Add("Còn hợp đồng/quyền thuê hoặc hồ sơ lịch sử khác chưa xử lý.");
        if (await db.MonthlyBills.AnyAsync(b => b.Contract.Post!.RoomId == roomId && b.Status != MonthlyBillStatus.Paid && b.Status != MonthlyBillStatus.Cancelled) ||
            await db.ContractOccupants.AnyAsync(o => o.Contract.Post!.RoomId == roomId && o.Status == ContractOccupantStatus.Active))
            reasons.Add("Phòng còn công nợ hoặc người ở của hồ sơ khác chưa xử lý.");
        if (await db.RentalRequests.AnyAsync(x => x.Post.RoomId == roomId && x.Id != c.RentalRequestId && x.Status == RentalRequestStatus.Approved) ||
            await db.Deposits.AnyAsync(d => d.RentalRequestId != c.RentalRequestId && (d.Status == DepositStatus.Paid || d.Status == DepositStatus.Confirmed || d.Status == DepositStatus.RefundRequested) &&
                !db.ContractSettlements.Any(s => s.DepositId == d.Id && s.Status == SettlementState.Completed) &&
                db.RentalRequests.Any(r => r.Id == d.RentalRequestId && r.Post.RoomId == roomId && r.Status != RentalRequestStatus.ConvertedToContract)))
            reasons.Add("Còn yêu cầu/cọc giữ chỗ khác.");
        foreach (var contract in await db.RentalContracts.Where(x => x.Post!.RoomId == roomId).ToListAsync())
            try { await ContractClosurePolicy.EnsureNoFinancialConflictAsync(db, contract); } catch (BusinessRuleException e) { if (!reasons.Contains(e.Message)) reasons.Add(e.Message); }
        return reasons;
    }

    public async Task<ContractClosureView> ExecuteAsync(int actorId, int contractId, string operation, ClosureCommandDto dto)
    {
        await ContractClosurePolicy.EnsureSchemaAsync(db);
        if (dto.IdempotencyKey.Length is < 16 or > 100) throw new BusinessRuleException("IdempotencyKey phải có 16–100 ký tự.");
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(dto))));
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await WorkflowLock.AcquireAsync(db);
        var u = await Actor(actorId); var c = await Contract(contractId);
        if (operation == "mediate") { if (u.RoleId != 0) throw BusinessRuleException.Forbidden("Chỉ Admin hỗ trợ đối soát."); }
        else Participant(u, c);
        var eventKey = "user:" + dto.IdempotencyKey;
        var prior = await db.ContractClosureEvents.AsNoTracking().FirstOrDefaultAsync(x => x.IdempotencyKey == eventKey);
        if (prior != null)
        {
            if (prior.ActorAccountId != actorId || prior.EventType != operation || !await db.ContractClosures.AnyAsync(h => h.Id == prior.ClosureId && h.ContractId == c.Id) ||
                JsonDocument.Parse(prior.SnapshotJson!).RootElement.GetProperty("Fingerprint").GetString() != fingerprint)
                throw BusinessRuleException.Conflict("IdempotencyKey đã được dùng cho nội dung khác.");
            await tx.CommitAsync(); return await GetAsync(actorId, contractId);
        }
        var existing = await db.ContractClosures.AnyAsync(x => x.ContractId == c.Id);
        var h = await ContractClosurePolicy.GetOrCreateAsync(db, c);
        if (existing) Version(h, dto.RowVersion);
        if (h.ReleasedAtUtc != null && operation != "mediate") throw BusinessRuleException.Conflict("Hồ sơ đã hoàn tất và giải phóng phòng.");
        var before = JsonSerializer.Serialize(h);
        var now = Now; var landlord = actorId == c.LandlordAccountId;
        var plan = await db.ContractSettlements.Include(x => x.Lines).FirstOrDefaultAsync(x => x.ClosureId == h.Id && x.Status != SettlementState.Superseded);
        if (operation is "request-end" or "agree-end" or "reject-end" or "withdraw-end")
            await EndRequest(c, h, actorId, operation, dto, now);
        else if (operation == "save-handover")
        {
            if (c.Status == RentalContractStatus.PendingSignature) throw BusinessRuleException.Conflict("Hợp đồng chưa được ký hoặc hủy.");
            await ContractClosurePolicy.BillChangingAsync(db, c.Id, actorId);
            if (dto.HandoverKind is not (Models.HandoverKind.Returned or Models.HandoverKind.NeverTaken)) throw new BusinessRuleException("Chọn hình thức bàn giao.");
            h.Condition = Required(dto.Condition, 2000); h.DamageNotes = dto.DamageNotes?.Trim(); h.EvidenceJson = Evidence(dto.EvidenceUrls, dto.HandoverKind == Models.HandoverKind.NeverTaken);
            if (dto.HandoverKind == Models.HandoverKind.Returned)
            {
                var at = dto.ActualHandoverAt?.UtcDateTime ?? throw new BusinessRuleException("Cần thời điểm trả phòng thực tế.");
                if (at > now || at < DateTime.SpecifyKind(c.StartDate.Date.Subtract(Helpers.RentalCalendar.VietnamOffset), DateTimeKind.Utc)) throw new BusinessRuleException("Ngày trả thực tế không hợp lệ.");
                if (dto.ClosingElectricity is null or < 0 || dto.ClosingWater is null or < 0) throw new BusinessRuleException("Cần chỉ số điện/nước trả phòng không âm.");
                var last = await db.MonthlyBills.Where(b => b.ContractId == c.Id && b.Status != MonthlyBillStatus.Cancelled).OrderByDescending(b => b.Year).ThenByDescending(b => b.Month).FirstOrDefaultAsync();
                if (last != null && (dto.ClosingElectricity < last.NewElectricity || dto.ClosingWater < last.NewWater)) throw BusinessRuleException.Conflict("Chỉ số trả phòng thấp hơn hóa đơn đã chốt.");
                h.ActualHandoverAtUtc = at; h.ClosingElectricity = dto.ClosingElectricity; h.ClosingWater = dto.ClosingWater;
            }
            else
            {
                if (await db.ContractOccupants.AnyAsync(o => o.ContractId == c.Id) || await db.MonthlyBills.AnyAsync(b => b.ContractId == c.Id && b.Status != MonthlyBillStatus.Cancelled))
                    throw BusinessRuleException.Conflict("Đã có người ở cùng/hóa đơn; không được xác nhận chưa nhận phòng.");
                h.ActualHandoverAtUtc = null; h.ClosingElectricity = null; h.ClosingWater = null;
            }
            h.HandoverKind = dto.HandoverKind; h.HandoverRevision++; h.HandoverStatus = HandoverState.PendingConfirmation;
            h.TenantHandoverConfirmedAtUtc = null; h.TenantHandoverConfirmedByAccountId = null; h.LandlordHandoverConfirmedAtUtc = null; h.LandlordHandoverConfirmedByAccountId = null;
            await ContractOccupancyPolicy.EndAsync(db, c.Id, c.Status == RentalContractStatus.Expired);
        }
        else if (operation is "confirm-handover" or "dispute-handover")
        {
            if (h.HandoverStatus != HandoverState.PendingConfirmation) throw BusinessRuleException.Conflict("Biên bản không chờ xác nhận.");
            if (operation == "dispute-handover") { Required(dto.Note, 2000); h.HandoverStatus = HandoverState.Disputed; }
            else
            {
                if (landlord) { if (h.LandlordHandoverConfirmedAtUtc != null) throw BusinessRuleException.Conflict("Bạn đã xác nhận."); h.LandlordHandoverConfirmedByAccountId = actorId; h.LandlordHandoverConfirmedAtUtc = now; }
                else { if (h.TenantHandoverConfirmedAtUtc != null) throw BusinessRuleException.Conflict("Bạn đã xác nhận."); h.TenantHandoverConfirmedByAccountId = actorId; h.TenantHandoverConfirmedAtUtc = now; }
                if (h.TenantHandoverConfirmedAtUtc != null && h.LandlordHandoverConfirmedAtUtc != null)
                {
                    h.HandoverStatus = HandoverState.Confirmed;
                    foreach (var o in await db.ContractOccupants.Where(o => o.ContractId == c.Id && o.Status == ContractOccupantStatus.Active).ToListAsync())
                    {
                        if (h.HandoverKind == Models.HandoverKind.NeverTaken || h.ActualHandoverAtUtc < o.JoinedAt) throw BusinessRuleException.Conflict("Biên bản mâu thuẫn lịch sử cư trú.");
                        o.Status = ContractOccupantStatus.Left; o.LeftAt = h.ActualHandoverAtUtc;
                    }
                }
            }
        }
        else if (operation == "finalize-bills")
        {
            if (!landlord || !Terminal(c) || h.EffectiveEndAtUtc == null || h.HandoverStatus != HandoverState.Confirmed) throw BusinessRuleException.Conflict("Cần chủ trọ, mốc kết thúc và bàn giao hai bên trước khi chốt công nợ.");
            if (plan?.Status == SettlementState.Completed || plan?.RefundReportedAtUtc != null || plan?.ExtraReportedAtUtc != null) throw BusinessRuleException.Conflict("Đã báo chuyển tiền/tất toán.");
            await ContractClosurePolicy.EnsureNoFinancialConflictAsync(db, c);
            if (h.HandoverKind == Models.HandoverKind.Returned && h.LastRentalDate >= c.StartDate.Date)
            {
                var last = await db.MonthlyBills.Where(b => b.ContractId == c.Id && b.Status != MonthlyBillStatus.Cancelled).OrderByDescending(b => b.Year).ThenByDescending(b => b.Month).FirstOrDefaultAsync();
                if (last == null || last.Year != h.LastRentalDate.Value.Year || last.Month != h.LastRentalDate.Value.Month || last.NewElectricity != h.ClosingElectricity || last.NewWater != h.ClosingWater)
                    throw BusinessRuleException.Conflict("Cần hóa đơn kỳ cuối đúng ngày thuê cuối và khớp chỉ số bàn giao.");
            }
            h.BillingFinalizedAtUtc = now; h.BillingFinalizedByAccountId = actorId;
        }
        else if (operation == "create-settlement")
        {
            if (!landlord || !Terminal(c) || h.EffectiveEndAtUtc == null || h.HandoverStatus != HandoverState.Confirmed || h.BillingFinalizedAtUtc == null) throw BusinessRuleException.Conflict("Cần kết thúc, bàn giao và chốt công nợ trước khi lập tất toán.");
            await ContractClosurePolicy.EnsureNoFinancialConflictAsync(db, c);
            if (plan?.Status == SettlementState.Completed || plan?.RefundReportedAtUtc != null || plan?.ExtraReportedAtUtc != null) throw BusinessRuleException.Conflict("Không được thay phương án đã chuyển tiền/hoàn tất.");
            var deposit = await db.Deposits.SingleOrDefaultAsync(d => d.RentalRequestId == c.RentalRequestId && d.Status == DepositStatus.Confirmed)
                ?? throw BusinessRuleException.Conflict("Chưa có cọc Confirmed hợp lệ; cần đối soát dữ liệu cũ.");
            if (deposit.Amount <= 0 || deposit.Amount != c.DepositAmount || deposit.TenantAccountId != c.TenantAccountId || deposit.LandlordAccountId != c.LandlordAccountId)
                throw BusinessRuleException.Conflict("Cọc không khớp số tiền/hai bên trong snapshot hợp đồng. Cần đối soát, không tự sửa lịch sử.");
            if (await db.RentalContracts.AnyAsync(x => x.Id != c.Id && x.RentalRequestId == c.RentalRequestId && RentalContractStatus.EffectiveStatuses.Contains(x.Status)) ||
                await db.ContractSettlements.AnyAsync(x => x.DepositId == deposit.Id && x.ClosureId != h.Id && x.Status != SettlementState.Superseded))
                throw BusinessRuleException.Conflict("Cọc đang được dùng bởi hợp đồng/hồ sơ khác.");
            if (plan != null) { plan.Status = SettlementState.Superseded; await db.SaveChangesAsync(); }
            var revision = await db.ContractSettlements.Where(x => x.ClosureId == h.Id).Select(x => (int?)x.Revision).MaxAsync() ?? 0;
            plan = new() { ClosureId = h.Id, DepositId = deposit.Id, Revision = revision + 1, BillingRevisionSnapshot = h.BillingRevision,
                DepositAmountSnapshot = deposit.Amount, ProposedByAccountId = actorId, CreatedAtUtc = now, Note = dto.Note?.Trim() };
            var bills = await db.MonthlyBills.Where(b => b.ContractId == c.Id && b.Status != MonthlyBillStatus.Paid && b.Status != MonthlyBillStatus.Cancelled).OrderBy(b => b.Year).ThenBy(b => b.Month).ToListAsync();
            foreach (var b in bills)
            {
                if (b.Status == MonthlyBillStatus.PendingPayment || b.TotalAmount <= 0) throw BusinessRuleException.Conflict("Hóa đơn đang xử lý hoặc số tiền không hợp lệ.");
                plan.Lines.Add(new() { Kind = 0, MonthlyBillId = b.Id, Amount = b.TotalAmount, Description = $"Hóa đơn {b.Month}/{b.Year}", BillTotalSnapshot = b.TotalAmount, BillStatusSnapshot = b.Status, BillUpdatedAtSnapshot = b.UpdatedAt });
            }
            if (dto.Damages.Count > 20) throw new BusinessRuleException("Tối đa 20 khoản thiệt hại.");
            foreach (var damage in dto.Damages)
            {
                Money(damage.Amount);
                plan.Lines.Add(new() { Kind = 1, Amount = damage.Amount, Description = Required(damage.Description), EvidenceJson = Evidence(damage.EvidenceUrls, true) });
            }
            plan.TotalCharges = plan.Lines.Sum(x => x.Amount); if (plan.TotalCharges > 9999999999999999.99m) throw new BusinessRuleException("Tổng tiền vượt giới hạn.");
            plan.RefundDue = Math.Max(0, deposit.Amount - plan.TotalCharges); plan.ExtraDue = Math.Max(0, plan.TotalCharges - deposit.Amount);
            var available = deposit.Amount;
            foreach (var line in plan.Lines) { line.DepositAppliedAmount = Math.Min(available, line.Amount); available -= line.DepositAppliedAmount; }
            db.ContractSettlements.Add(plan);
        }
        else if (operation.EndsWith("settlement", StringComparison.Ordinal) || operation is "report-refund" or "confirm-refund" or "report-extra" or "confirm-extra")
        {
            if (plan == null || dto.TargetId != plan.Id) throw BusinessRuleException.Conflict("Phương án đã thay đổi. Vui lòng tải lại.");
            await ActSettlement(c, h, plan, actorId, operation, dto, now);
        }
        else if (operation == "release")
        {
            if (!landlord) throw BusinessRuleException.Forbidden("Chỉ chủ trọ của phòng được xác nhận sẵn sàng.");
            var blockers = await Blockers(c, h, plan); if (blockers.Count > 0) throw BusinessRuleException.Conflict(string.Join(" ", blockers));
            h.ReleasedAtUtc = now; h.ReleasedByAccountId = actorId;
            foreach (var related in await db.ContractClosures.Where(x => x.RoomId == h.RoomId && x.Id != h.Id && x.ReleasedAtUtc == null).ToListAsync())
            {
                related.ReleasedAtUtc = now; related.ReleasedByAccountId = actorId; related.UpdatedAtUtc = now;
                ContractClosurePolicy.Event(db, related.Id, "related-room-release", actorId, reason: $"Cùng phòng với hồ sơ #{h.Id}, đã kiểm tra đầy đủ bàn giao/tất toán.", now: now);
            }
            if (c.Post!.Room.Status != RoomStatus.TemporarilyUnavailable) c.Post.Room.Status = RoomStatus.Available;
            var r = await db.RentalRequests.SingleAsync(x => x.Id == c.RentalRequestId);
            if (c.Status == RentalContractStatus.Cancelled && !await db.RentalContracts.AnyAsync(x => x.Id != c.Id && x.RentalRequestId == r.Id && RentalContractStatus.EffectiveStatuses.Contains(x.Status))) r.Status = RentalRequestStatus.Cancelled;
        }
        else if (operation == "mediate") { Required(dto.Note, 2000); Evidence(dto.EvidenceUrls, true); }
        else throw new BusinessRuleException("Thao tác không hợp lệ.");
        h.UpdatedAtUtc = now;
        await db.SaveChangesAsync();
        if (operation is "confirm-handover" or "save-handover") { await ContractOccupancyPolicy.RecalculateCurrentOccupantsAsync(db, h.RoomId); await db.SaveChangesAsync(); }
        ContractClosurePolicy.Event(db, h.Id, operation, actorId, reason: dto.Note?.Trim(), snapshot: JsonSerializer.Serialize(new { Fingerprint = fingerprint, Command = dto, Before = JsonDocument.Parse(before).RootElement, After = h, Settlement = plan }), key: eventKey, now: now);
        await db.SaveChangesAsync(); await tx.CommitAsync();
        try
        {
            foreach (var recipient in new[] { c.TenantAccountId, c.LandlordAccountId }.Where(x => x != actorId))
                await notifications.CreateNotificationAsync(recipient, "Cập nhật kết thúc hợp đồng", $"Hợp đồng #{c.Id}: {operation}.", 1, recipient == c.TenantAccountId ? "/tenant/rentals" : "/landlord/contracts?tab=contracts");
        }
        catch (Exception e) { logger.LogWarning(e, "Không gửi được thông báo kết thúc hợp đồng {ContractId}", c.Id); }
        return await GetAsync(actorId, contractId);
    }

    private static void Money(decimal amount)
    { if (amount <= 0 || amount > 9999999999999999.99m || decimal.Round(amount, 2) != amount) throw new BusinessRuleException("Số tiền phải dương, tối đa hai chữ số thập phân và không vượt decimal(18,2)."); }

    private async Task EndRequest(RentalContract c, ContractClosure h, int actorId, string op, ClosureCommandDto dto, DateTime now)
    {
        var legacy = c.Status is RentalContractStatus.Terminated or RentalContractStatus.Cancelled && h.EffectiveEndAtUtc == null;
        if (c.Status is not (RentalContractStatus.Active or RentalContractStatus.PendingStart) && !legacy) throw BusinessRuleException.Conflict("Hợp đồng không còn được đề nghị kết thúc sớm.");
        var pending = await db.ContractEndRequests.FirstOrDefaultAsync(x => x.ClosureId == h.Id && (x.Status == 0 || x.Status == 1));
        if (op == "request-end")
        {
            if (pending != null) throw BusinessRuleException.Conflict("Đã có đề nghị kết thúc đang xử lý.");
            var at = dto.EffectiveEndAt?.UtcDateTime ?? throw new BusinessRuleException("Cần thời điểm hiệu lực có múi giờ.");
            if ((!legacy && at < now.AddMinutes(-5)) || at > ContractClosurePolicy.NaturalEndUtc(c) || at.Year < 2000) throw new BusinessRuleException("Thời điểm hiệu lực không hợp lệ hoặc sau ngày hết hạn.");
            if (legacy) Evidence(dto.EvidenceUrls, true);
            db.ContractEndRequests.Add(new() { ClosureId = h.Id, RequestedByAccountId = actorId, RequestedAtUtc = now, Reason = Required(dto.Note), ProposedEffectiveEndAtUtc = at });
            return;
        }
        if (pending == null || pending.Status != ContractEndState.Requested || dto.TargetId != pending.Id) throw BusinessRuleException.Conflict("Đề nghị đã thay đổi hoặc không còn chờ phản hồi.");
        if (op == "withdraw-end") { if (pending.RequestedByAccountId != actorId) throw BusinessRuleException.Forbidden("Chỉ người gửi được rút đề nghị."); pending.Status = ContractEndState.Withdrawn; return; }
        if (pending.RequestedByAccountId == actorId) throw BusinessRuleException.Forbidden("Không được tự duyệt đề nghị của mình.");
        if (op == "agree-end" && !legacy && now >= ContractClosurePolicy.NaturalEndUtc(c)) throw BusinessRuleException.Conflict("Hợp đồng đã hết thời hạn tự nhiên. Chờ đồng bộ hết hạn và xử lý bàn giao, không chấm dứt sớm hồi tố.");
        pending.RespondedByAccountId = actorId; pending.RespondedAtUtc = now; pending.ResponseNote = dto.Note?.Trim();
        if (op == "reject-end") { Required(dto.Note); pending.Status = ContractEndState.Rejected; return; }
        pending.Status = ContractEndState.Agreed; pending.AgreedEffectiveEndAtUtc = pending.ProposedEffectiveEndAtUtc;
        if (pending.ProposedEffectiveEndAtUtc <= now)
        {
            var previousStatus = c.Status;
            if (!legacy) c.Status = RentalContractStatus.Terminated;
            h.EndKind = c.Status == RentalContractStatus.Cancelled ? (byte)2 : (byte)1;
            h.EffectiveEndAtUtc = pending.ProposedEffectiveEndAtUtc;
            h.LastRentalDate = c.Status == RentalContractStatus.Cancelled ? c.StartDate.Date.AddDays(-1) : ContractClosurePolicy.LastDay(h.EffectiveEndAtUtc.Value);
            pending.Status = ContractEndState.Effective; pending.EffectiveAtUtc = now;
            await ContractOccupancyPolicy.EndAsync(db, c.Id, false);
            if (!legacy) ContractClosurePolicy.Event(db, h.Id, "contract-ended", null, from: previousStatus.ToString(), to: c.Status.ToString(), key: $"contract-ended:{c.Id}", now: now);
        }
    }

    private async Task ActSettlement(RentalContract c, ContractClosure h, ContractSettlement p, int actor, string op, ClosureCommandDto dto, DateTime now)
    {
        var landlord = actor == c.LandlordAccountId;
        if (p.Status == SettlementState.Completed || p.Status == SettlementState.Superseded) throw BusinessRuleException.Conflict("Phương án đã hoàn tất/bị thay thế.");
        if (op == "dispute-settlement")
        {
            if (p.Status is not (SettlementState.Proposed or SettlementState.Agreed or SettlementState.TransferReported)) throw BusinessRuleException.Conflict("Phương án không ở bước được phản hồi tranh chấp.");
            Required(dto.Note, 2000); p.Status = SettlementState.Disputed;
            p.TenantAgreedByAccountId = null; p.TenantAgreedAtUtc = null; p.LandlordAgreedByAccountId = null; p.LandlordAgreedAtUtc = null;
            return;
        }
        if (h.HandoverStatus != HandoverState.Confirmed || h.BillingFinalizedAtUtc == null || p.BillingRevisionSnapshot != h.BillingRevision) throw BusinessRuleException.Conflict("Bàn giao/công nợ đã thay đổi.");
        await ContractClosurePolicy.EnsureNoFinancialConflictAsync(db, c);
        if (op == "resume-settlement")
        {
            if (p.Status != SettlementState.Disputed) throw BusinessRuleException.Conflict("Phương án không có tranh chấp chờ đồng thuận lại.");
            if (landlord) { if (p.LandlordAgreedAtUtc != null) throw BusinessRuleException.Conflict("Bạn đã đồng ý lại."); p.LandlordAgreedByAccountId = actor; p.LandlordAgreedAtUtc = now; }
            else { if (p.TenantAgreedAtUtc != null) throw BusinessRuleException.Conflict("Bạn đã đồng ý lại."); p.TenantAgreedByAccountId = actor; p.TenantAgreedAtUtc = now; }
            if (p.TenantAgreedAtUtc != null && p.LandlordAgreedAtUtc != null) p.Status = p.RefundReportedAtUtc != null || p.ExtraReportedAtUtc != null ? SettlementState.TransferReported : SettlementState.Agreed;
            return;
        }
        if (op == "propose-settlement")
        {
            if (p.Status != SettlementState.Draft || actor != p.ProposedByAccountId) throw BusinessRuleException.Conflict("Không được gửi phương án này.");
            p.Status = SettlementState.Proposed; p.ProposedAtUtc = now; return;
        }
        if (op == "agree-settlement")
        {
            if (p.Status != SettlementState.Proposed) throw BusinessRuleException.Conflict("Phương án không chờ đồng thuận.");
            if (landlord) { if (p.LandlordAgreedAtUtc != null) throw BusinessRuleException.Conflict("Bạn đã đồng ý."); p.LandlordAgreedByAccountId = actor; p.LandlordAgreedAtUtc = now; }
            else { if (p.TenantAgreedAtUtc != null) throw BusinessRuleException.Conflict("Bạn đã đồng ý."); p.TenantAgreedByAccountId = actor; p.TenantAgreedAtUtc = now; }
            if (p.TenantAgreedAtUtc != null && p.LandlordAgreedAtUtc != null) p.Status = SettlementState.Agreed;
            return;
        }
        if (p.Status is not (SettlementState.Agreed or SettlementState.TransferReported)) throw BusinessRuleException.Conflict("Cần hai bên đồng thuận phương án trước khi chuyển/nhận tiền.");
        switch (op)
        {
            case "report-refund" when landlord && p.RefundDue > 0 && p.RefundReportedAtUtc == null:
                p.RefundReference = Required(dto.Reference, 500); p.RefundReportedByAccountId = actor; p.RefundReportedAtUtc = now; p.Status = SettlementState.TransferReported; break;
            case "confirm-refund" when !landlord && p.RefundReportedAtUtc != null && p.RefundReceivedAtUtc == null:
                p.RefundReceivedByAccountId = actor; p.RefundReceivedAtUtc = now; break;
            case "report-extra" when !landlord && p.ExtraDue > 0 && p.ExtraReportedAtUtc == null:
                p.ExtraReference = Required(dto.Reference, 500); p.ExtraReportedByAccountId = actor; p.ExtraReportedAtUtc = now; p.Status = SettlementState.TransferReported; break;
            case "confirm-extra" when landlord && p.ExtraReportedAtUtc != null && p.ExtraReceivedAtUtc == null:
                p.ExtraReceivedByAccountId = actor; p.ExtraReceivedAtUtc = now; break;
            case "complete-settlement":
                if (p.RefundDue > 0 && p.RefundReceivedAtUtc == null || p.ExtraDue > 0 && p.ExtraReceivedAtUtc == null) throw BusinessRuleException.Conflict("Chưa xác nhận nhận đủ tiền thực tế.");
                var bills = await db.MonthlyBills.Where(b => b.ContractId == c.Id && b.Status != MonthlyBillStatus.Paid && b.Status != MonthlyBillStatus.Cancelled).ToListAsync();
                var lines = p.Lines.Where(x => x.Kind == 0).ToList();
                if (bills.Count != lines.Count || bills.Any(b => !lines.Any(l => l.MonthlyBillId == b.Id && l.BillTotalSnapshot == b.TotalAmount && l.Amount == b.TotalAmount && l.BillUpdatedAtSnapshot == b.UpdatedAt) || b.Status == MonthlyBillStatus.PendingPayment))
                    throw BusinessRuleException.Conflict("Hóa đơn thay đổi so với phương án. Cần đối soát và đồng thuận lại.");
                var deposit = await db.Deposits.SingleAsync(d => d.Id == p.DepositId);
                if (deposit.Status != DepositStatus.Confirmed || deposit.Amount != p.DepositAmountSnapshot || deposit.Amount != c.DepositAmount ||
                    deposit.TenantAccountId != c.TenantAccountId || deposit.LandlordAccountId != c.LandlordAccountId) throw BusinessRuleException.Conflict("Cọc thay đổi; cần đối soát.");
                foreach (var b in bills) { b.Status = MonthlyBillStatus.Paid; b.PaidAt = now; b.PaymentMethod = "ContractSettlement"; }
                p.Status = SettlementState.Completed; p.CompletedAtUtc = now; p.CompletedByAccountId = actor; break;
            default: throw BusinessRuleException.Forbidden("Sai bên xử lý hoặc trạng thái chuyển/nhận tiền không hợp lệ.");
        }
    }
}
