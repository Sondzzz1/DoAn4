namespace RoomRental.BackEnd.DTO.Payment;

public class CreatePaymentRequestDto
{
    public int DepositId { get; set; }
    public string? OrderInfo { get; set; }
}

public class PaymentResponseDto
{
    public string PaymentUrl { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class PaymentResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string ResponseCode { get; set; } = string.Empty;
    public int? DepositId { get; set; }
}
