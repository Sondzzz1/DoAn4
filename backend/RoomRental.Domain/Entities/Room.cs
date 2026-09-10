using RoomRental.Domain.Enums;

namespace RoomRental.Domain.Entities;

/// <summary>
/// Entity Room - Thông tin phòng trọ
/// Quan hệ 1-1 với Post (Mỗi Post có 1 Room)
/// </summary>
public class Room
{
    /// <summary>
    /// ID của Room - Primary Key
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign Key - ID của Post
    /// Quan hệ 1-1: 1 Room thuộc về 1 Post
    /// </summary>
    public int PostId { get; set; }

    /// <summary>
    /// Diện tích phòng (m²)
    /// </summary>
    public decimal Area { get; set; }

    /// <summary>
    /// Số người tối đa có thể ở
    /// </summary>
    public int MaxOccupants { get; set; }

    /// <summary>
    /// Trạng thái phòng: Available, Rented, TemporarilyUnavailable
    /// </summary>
    public RoomStatus Status { get; set; } = RoomStatus.Available;

    /// <summary>
    /// Tỉnh/Thành phố
    /// Ví dụ: "Hồ Chí Minh", "Hà Nội"
    /// </summary>
    public string Province { get; set; } = string.Empty;

    /// <summary>
    /// Quận/Huyện
    /// Ví dụ: "Quận 7", "Huyện Bình Chánh"
    /// </summary>
    public string District { get; set; } = string.Empty;

    /// <summary>
    /// Phường/Xã
    /// Ví dụ: "Phường Tân Phú", "Xã Tân Kiên"
    /// </summary>
    public string Ward { get; set; } = string.Empty;

    /// <summary>
    /// Địa chỉ chi tiết
    /// Ví dụ: "123 Nguyễn Văn Linh"
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Ngày tạo
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Ngày cập nhật lần cuối
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    /// <summary>
    /// Tin đăng sở hữu phòng này (quan hệ 1-1)
    /// </summary>
    public virtual Post Post { get; set; } = null!;
}
