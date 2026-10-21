namespace RoomRental.BackEnd.Models;

public static class CashPaymentStatus
{
    public const int Selected = 0;
    public const int Received = 1;
    public const int Cancelled = 2;
}

/// <summary>A cash choice becomes a receipt only after the responsible landlord confirms actual receipt.</summary>
public class CashPaymentReceipt
{
    public int Id { get; set; }
    public int? DepositId { get; set; }
    public int? MonthlyBillId { get; set; }
    public decimal Amount { get; set; }
    public int TenantAccountId { get; set; }
    public int LandlordAccountId { get; set; }
    public int Status { get; set; } = CashPaymentStatus.Selected;
    public DateTime SelectedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public int? ConfirmedByAccountId { get; set; }
    public string? ReceiptNumber { get; set; }
    public string? Note { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
