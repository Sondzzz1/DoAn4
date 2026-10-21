using System.ComponentModel.DataAnnotations;

namespace RoomRental.BackEnd.DTO.Payment;

public class SavedPaymentMethodDto
{
    public int Id { get; set; }
    public string BankCode { get; set; } = "";
    public string MaskedCardNumber { get; set; } = "";
    public string CardType { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
}

public class SavedPaymentMethodsDto
{
    public bool Enabled { get; set; }
    public string? UnavailableReason { get; set; }
    public List<SavedPaymentMethodDto> Methods { get; set; } = [];
}

public class VnPayCardLinkRequestDto
{
    [RegularExpression("^(01|02)$")]
    public string CardType { get; set; } = "01";
}

public class VnPayCardLinkResponseDto
{
    public string RedirectUrl { get; set; } = "";
    public string Reference { get; set; } = "";
}
