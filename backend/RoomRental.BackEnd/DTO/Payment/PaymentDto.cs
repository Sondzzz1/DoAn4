using System.Text.Json.Serialization;

namespace RoomRental.BackEnd.DTO.Payment;

public enum PaymentCallbackOutcome
{
    NotProcessed,
    Recorded,
    AlreadyRecorded,
    OrderNotFound,
    InvalidAmount,
    InvalidSignature
}

public sealed class VnPayIpnResponseDto
{
    [JsonPropertyName("RspCode")]
    public string RspCode { get; init; } = "99";
    [JsonPropertyName("Message")]
    public string Message { get; init; } = "Unknown error";
}

public class CreatePaymentRequestDto
{
    public int? DepositId { get; set; }
    public int? MonthlyBillId { get; set; }
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
    // An IPN acknowledgement is distinct from whether the customer's payment succeeded.
    [JsonIgnore]
    public PaymentCallbackOutcome CallbackOutcome { get; set; }

    public bool Success { get; set; }
    public bool RequiresReview { get; set; }
    public long? PaymentEventId { get; set; }
    public byte? EventState { get; set; }
    public string Message { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string ResponseCode { get; set; } = string.Empty;
    public int? DepositId { get; set; }
    public int? MonthlyBillId { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string TargetType { get; set; } = "Deposit";
}
