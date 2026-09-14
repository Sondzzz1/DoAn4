using RoomRental.Domain.Enums;

namespace RoomRental.Application.DTOs.Post;

/// <summary>
/// DTO trả về thông tin Post
/// </summary>
public class PostDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public PostStatus Status { get; set; }
    public string? RejectionReason { get; set; }

    // Landlord Info
    public int LandlordId { get; set; }
    public string LandlordName { get; set; } = string.Empty;
    public string LandlordPhone { get; set; } = string.Empty;

    // Room Info
    public int RoomId { get; set; }
    public decimal Area { get; set; }
    public int MaxOccupants { get; set; }
    public RoomStatus RoomStatus { get; set; }
    public string Province { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Ward { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;

    // Amenities
    public List<AmenityDto> Amenities { get; set; } = new();

    // Images
    public List<string> ImageUrls { get; set; } = new();

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO cho Amenity trong Post
/// </summary>
public class AmenityDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
