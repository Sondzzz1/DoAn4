using System.ComponentModel.DataAnnotations;

namespace RoomRental.BackEnd.DTO.Payment;

public class CashPaymentSelectDto
{
    public int? DepositId { get; set; }
    public int? MonthlyBillId { get; set; }
}

public class CashPaymentConfirmDto
{
    [MaxLength(1000)]
    public string? Note { get; set; }
}

public class CashPaymentDto
{
    public int Id { get; set; }
    public int? DepositId { get; set; }
    public int? MonthlyBillId { get; set; }
    public decimal Amount { get; set; }
    public int Status { get; set; }
    public int TenantAccountId { get; set; }
    public int LandlordAccountId { get; set; }
    public string TenantName { get; set; } = "";
    public string LandlordName { get; set; } = "";
    public string? LandlordPhone { get; set; }
    public string TargetTitle { get; set; } = "";
    public DateTime SelectedAtUtc { get; set; }
    public DateTime? ConfirmedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public int? ConfirmedByAccountId { get; set; }
    public string? ReceiptNumber { get; set; }
    public string? Note { get; set; }
    public DateTime? TargetDueAt { get; set; }
    public bool CanConfirm { get; set; }
    public string? UnavailableReason { get; set; }
}
