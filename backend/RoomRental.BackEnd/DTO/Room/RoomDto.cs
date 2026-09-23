using RoomRental.BackEnd.Models.Enums;

namespace RoomRental.BackEnd.DTO.Room;

public class RoomDto
{
    public int Id { get; set; }
    public int LandlordId { get; set; }
    public string LandlordName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal Area { get; set; }
    public int MaxOccupants { get; set; }
    public int CurrentOccupants { get; set; }
    public int? Bedrooms { get; set; }
    public int? Bathrooms { get; set; }
    public int? Floor { get; set; }
    public string Address { get; set; } = string.Empty;
    public string? Ward { get; set; }
    public string? District { get; set; }
    public string? Province { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public decimal? ElectricityPrice { get; set; }
    public decimal? WaterPrice { get; set; }
    public decimal? ServiceFee { get; set; }
    public RoomStatus Status { get; set; }
    public string StatusText => Status.ToString();

    public List<string> ImageUrls { get; set; } = new();
    public List<int> AmenityIds { get; set; } = new();

    public int? ActivePostId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
