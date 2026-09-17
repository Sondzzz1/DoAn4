using RoomRental.Domain.Enums;

namespace RoomRental.Application.DTOs.Post;

/// <summary>
/// DTO trả về danh sách Post dạng tóm tắt
/// </summary>
public class PostListDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public PostStatus Status { get; set; }
    public string StatusText => Status.ToString();
    public decimal Area { get; set; }
    public int MaxOccupants { get; set; }
    public RoomStatus RoomStatus { get; set; }
    public string Province { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Ward { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int LandlordId { get; set; }
    public string LandlordName { get; set; } = string.Empty;
    public string? LandlordPhone { get; set; }
    public int ViewCount { get; set; }
    public DateTime? PostedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Vietnamese aliases
    public string TieuDe => Title;
    public decimal Gia => Price;
    public decimal DienTich => Area;
    public string DiaChi => Address;
    public string Phuong => Ward;
    public string Quan => District;
    public string ThanhPho => Province;
}
