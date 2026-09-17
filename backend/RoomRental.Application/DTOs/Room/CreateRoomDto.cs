namespace RoomRental.Application.DTOs.Room;

public class CreateRoomDto
{
    public int CategoryId { get; set; } = 1;
    public string RoomName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal Area { get; set; }
    public int MaxOccupants { get; set; } = 2;
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

    public List<int> AmenityIds { get; set; } = new();
    public List<string> ImageUrls { get; set; } = new();
}

public class UpdateRoomDto : CreateRoomDto
{
}
