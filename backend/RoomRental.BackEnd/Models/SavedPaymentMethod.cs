namespace RoomRental.BackEnd.Models;

public class SavedPaymentMethod
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string MerchantCode { get; set; } = "";
    public string? TokenCiphertext { get; set; }
    public string TokenFingerprint { get; set; } = "";
    public string BankCode { get; set; } = "";
    public string MaskedCardNumber { get; set; } = "";
    public string CardType { get; set; } = "01";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UnlinkedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public static class VnPayCardLinkStatus
{
    public const int Pending = 0;
    public const int Completed = 1;
    public const int Failed = 2;
}

public class VnPayCardLink
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Reference { get; set; } = "";
    public string MerchantCode { get; set; } = "";
    public string CardType { get; set; } = "01";
    public int Status { get; set; } = VnPayCardLinkStatus.Pending;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public int? SavedPaymentMethodId { get; set; }
    public virtual SavedPaymentMethod? SavedPaymentMethod { get; set; }
    public string? CallbackFingerprint { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
