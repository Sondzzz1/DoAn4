using System.ComponentModel.DataAnnotations;

namespace RoomRental.BackEnd.DTO.Payment;

public class DepositRefundDto
{
    public int Id { get; set; }
    public int DepositId { get; set; }
    public int RentalRequestId { get; set; }
    public string RoomName { get; set; } = "";
    public decimal Amount { get; set; }
    public byte Status { get; set; }
    public string Reason { get; set; } = "";
    public string? DecisionNote { get; set; }
    public string? RefundReference { get; set; }
    public DateTime RequestedAtUtc { get; set; }
    public DateTime? RefundReportedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string RowVersion { get; set; } = "";
}

public class RefundCreateDto
{
    public int DepositId { get; set; }
    [Required, StringLength(500, MinimumLength = 5)] public string Reason { get; set; } = "";
}

public class RefundActionDto
{
    [StringLength(1000)] public string? Note { get; set; }
    [StringLength(200)] public string? Reference { get; set; }
    public string? RowVersion { get; set; }
}

public class PaymentEventDto
{
    public long Id { get; set; }
    public string OrderId { get; set; } = "";
    public string? TransactionCode { get; set; }
    public decimal ReportedAmount { get; set; }
    public decimal? ExpectedAmount { get; set; }
    public byte Source { get; set; }
    public string ResponseCode { get; set; } = "";
    public string ProviderStatus { get; set; } = "";
    public byte ProcessingState { get; set; }
    public string? Reason { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
    public DateTime? ProviderPaidAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public string? ResolutionNote { get; set; }
    public string RowVersion { get; set; } = "";
}
