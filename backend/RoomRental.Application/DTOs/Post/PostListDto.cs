using RoomRental.Domain.Enums;

namespace RoomRental.Application.DTOs.Post;

/// <summary>
/// DTO rút gọn cho danh sách Post (không có full detail)
/// </summary>
public class PostListDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public PostStatus Status { get; set; }

    // Room Info
    public decimal Area { get; set; }
    public int MaxOccupants { get; set; }
    public RoomStatus RoomStatus { get; set; }
    
    // Location
    public string Province { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Ward { get; set; } = string.Empty;

    // First Image
    public string? ThumbnailUrl { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
