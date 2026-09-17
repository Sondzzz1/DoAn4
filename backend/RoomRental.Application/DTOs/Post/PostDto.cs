using RoomRental.Application.DTOs.Amenity;
using RoomRental.Domain.Enums;

namespace RoomRental.Application.DTOs.Post;

/// <summary>
/// DTO trả về chi tiết bài đăng
/// </summary>
public class PostDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public PostStatus Status { get; set; }
    public string StatusText => Status.ToString();
    public string? RejectionReason { get; set; }
    public int ViewCount { get; set; }

    // Category
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;

    // Landlord Info
    public int LandlordId { get; set; }
    public int LandlordAccountId { get; set; }
    public string LandlordName { get; set; } = string.Empty;
    public string LandlordPhone { get; set; } = string.Empty;
    public string? LandlordAvatar { get; set; }

    // Room Info
    public int RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public decimal Area { get; set; }
    public int MaxOccupants { get; set; }
    public int CurrentOccupants { get; set; }
    public int? Bedrooms { get; set; }
    public int? Bathrooms { get; set; }
    public int? Floor { get; set; }
    public RoomStatus RoomStatus { get; set; }

    // Address & Location
    public string Province { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Ward { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    // Costs
    public decimal? ElectricityPrice { get; set; }
    public decimal? WaterPrice { get; set; }
    public decimal? ServiceFee { get; set; }

    // Amenities & Images
    public List<AmenityDto> Amenities { get; set; } = new();
    public List<string> ImageUrls { get; set; } = new();

    public DateTime? PostedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Vietnamese aliases for frontend compatibility
    public string TieuDe => Title;
    public decimal Gia => Price;
    public decimal DienTich => Area;
    public string DiaChi => Address;
    public string Phuong => Ward;
    public string Quan => District;
    public string ThanhPho => Province;
    public int SoNguoiToiDa => MaxOccupants;
}
