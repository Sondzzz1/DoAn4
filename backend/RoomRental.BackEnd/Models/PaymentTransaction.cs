namespace RoomRental.BackEnd.Models;

public class PaymentTransaction
{
    public int Id { get; set; }
    public int? DepositId { get; set; }
    public int? MonthlyBillId { get; set; }
    public string TargetType { get; set; } = "Deposit";
    public string OrderId { get; set; } = string.Empty;
    public string? TransactionCode { get; set; }
    public decimal Amount { get; set; }
    public int Status { get; set; }
    public string? ResponseCode { get; set; }
    public string? OrderInfo { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? ProcessedAt { get; set; }

    public virtual Deposit? Deposit { get; set; }
    public virtual MonthlyBill? MonthlyBill { get; set; }
}
