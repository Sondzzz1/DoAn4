namespace RoomRental.BackEnd.Models;

public static class PaymentEventSource { public const byte Ipn = 0, Return = 1; }
public static class PaymentEventState { public const byte Recorded = 0, Applied = 1, RequiresReview = 2, Resolved = 3; }
public static class DepositRefundStatus
{
    public const byte Requested = 0, Approved = 1, RefundReported = 2, Completed = 3, Rejected = 4, Cancelled = 5;
}

public class PaymentCallbackEvent
{
    public long Id { get; set; }
    public int? PaymentTransactionId { get; set; }
    public string ProviderOrderId { get; set; } = "";
    public string? ProviderTransactionCode { get; set; }
    public string DeduplicationKey { get; set; } = "";
    public byte Source { get; set; }
    public decimal ReportedAmount { get; set; }
    public string ResponseCode { get; set; } = "";
    public string ProviderStatus { get; set; } = "";
    public DateTime? ProviderPaidAtUtc { get; set; }
    public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;
    public byte ProcessingState { get; set; }
    public string? Reason { get; set; }
    public int? ResolvedByAccountId { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public string? ResolutionNote { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public virtual PaymentTransaction? PaymentTransaction { get; set; }
}

public class DepositRefundRequest
{
    public int Id { get; set; }
    public int DepositId { get; set; }
    public decimal Amount { get; set; }
    public int PreviousDepositStatus { get; set; }
    public byte Status { get; set; }
    public string Reason { get; set; } = "";
    public int RequestedByAccountId { get; set; }
    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
    public int? ReviewedByAccountId { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? DecisionNote { get; set; }
    public string? RefundReference { get; set; }
    public int? RefundReportedByAccountId { get; set; }
    public DateTime? RefundReportedAtUtc { get; set; }
    public int? CompletedByAccountId { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public virtual Deposit Deposit { get; set; } = null!;
}
