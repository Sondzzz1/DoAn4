using System.Text.Json.Serialization;

namespace RoomRental.BackEnd.Models;

public static class HandoverState { public const byte NotRecorded = 0, Draft = 1, PendingConfirmation = 2, Confirmed = 3, Disputed = 4; }
public static class HandoverKind { public const byte Returned = 0, NeverTaken = 1; }
public static class ContractEndState { public const byte Requested = 0, Agreed = 1, Rejected = 2, Withdrawn = 3, Superseded = 4, Effective = 5; }
public static class SettlementState { public const byte Draft = 0, Proposed = 1, Agreed = 2, TransferReported = 3, Completed = 4, Disputed = 5, Superseded = 6; }

public class ContractClosure
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public int RoomId { get; set; }
    public byte? EndKind { get; set; }
    public DateTime? EffectiveEndAtUtc { get; set; }
    public DateTime? LastRentalDate { get; set; }
    public byte HandoverStatus { get; set; }
    public byte? HandoverKind { get; set; }
    public int HandoverRevision { get; set; }
    public DateTime? ActualHandoverAtUtc { get; set; }
    public string? Condition { get; set; }
    public decimal? ClosingElectricity { get; set; }
    public decimal? ClosingWater { get; set; }
    public string? DamageNotes { get; set; }
    public string? EvidenceJson { get; set; }
    public int? TenantHandoverConfirmedByAccountId { get; set; }
    public DateTime? TenantHandoverConfirmedAtUtc { get; set; }
    public int? LandlordHandoverConfirmedByAccountId { get; set; }
    public DateTime? LandlordHandoverConfirmedAtUtc { get; set; }
    public int BillingRevision { get; set; }
    public DateTime? BillingFinalizedAtUtc { get; set; }
    public int? BillingFinalizedByAccountId { get; set; }
    public DateTime? ReleasedAtUtc { get; set; }
    public int? ReleasedByAccountId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
    [JsonIgnore] public RentalContract Contract { get; set; } = null!;
}

public class ContractEndRequest
{
    public int Id { get; set; }
    public int ClosureId { get; set; }
    public byte Status { get; set; }
    public int RequestedByAccountId { get; set; }
    public DateTime RequestedAtUtc { get; set; }
    public string Reason { get; set; } = "";
    public DateTime ProposedEffectiveEndAtUtc { get; set; }
    public int? RespondedByAccountId { get; set; }
    public DateTime? RespondedAtUtc { get; set; }
    public string? ResponseNote { get; set; }
    public DateTime? AgreedEffectiveEndAtUtc { get; set; }
    public DateTime? EffectiveAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public class ContractSettlement
{
    public int Id { get; set; }
    public int ClosureId { get; set; }
    public int DepositId { get; set; }
    public int Revision { get; set; }
    public byte Status { get; set; }
    public int BillingRevisionSnapshot { get; set; }
    public decimal DepositAmountSnapshot { get; set; }
    public decimal TotalCharges { get; set; }
    public decimal RefundDue { get; set; }
    public decimal ExtraDue { get; set; }
    public int ProposedByAccountId { get; set; }
    public DateTime? ProposedAtUtc { get; set; }
    public int? TenantAgreedByAccountId { get; set; }
    public DateTime? TenantAgreedAtUtc { get; set; }
    public int? LandlordAgreedByAccountId { get; set; }
    public DateTime? LandlordAgreedAtUtc { get; set; }
    public string? RefundReference { get; set; }
    public int? RefundReportedByAccountId { get; set; }
    public DateTime? RefundReportedAtUtc { get; set; }
    public int? RefundReceivedByAccountId { get; set; }
    public DateTime? RefundReceivedAtUtc { get; set; }
    public string? ExtraReference { get; set; }
    public int? ExtraReportedByAccountId { get; set; }
    public DateTime? ExtraReportedAtUtc { get; set; }
    public int? ExtraReceivedByAccountId { get; set; }
    public DateTime? ExtraReceivedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public int? CompletedByAccountId { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public List<ContractSettlementLine> Lines { get; set; } = [];
}

public class ContractSettlementLine
{
    public int Id { get; set; }
    public int SettlementId { get; set; }
    public byte Kind { get; set; }
    public int? MonthlyBillId { get; set; }
    public decimal Amount { get; set; }
    public decimal DepositAppliedAmount { get; set; }
    public string Description { get; set; } = "";
    public string? EvidenceJson { get; set; }
    public int? BillStatusSnapshot { get; set; }
    public DateTime? BillUpdatedAtSnapshot { get; set; }
    public decimal? BillTotalSnapshot { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public class ContractClosureEvent
{
    public long Id { get; set; }
    public int ClosureId { get; set; }
    public int? EndRequestId { get; set; }
    public int? SettlementId { get; set; }
    public string EventType { get; set; } = "";
    public int? ActorAccountId { get; set; }
    public byte Source { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public string? FromState { get; set; }
    public string? ToState { get; set; }
    public string? Reason { get; set; }
    public string? SnapshotJson { get; set; }
    public string IdempotencyKey { get; set; } = "";
}
