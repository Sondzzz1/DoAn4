namespace RoomRental.Application.DTOs.Amenity;

public class AmenityDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class CreateAmenityDto
{
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateAmenityDto : CreateAmenityDto
{
}
