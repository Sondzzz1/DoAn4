namespace RoomRental.Domain.Entities;

/// <summary>
/// Entity PostAmenity - Bảng trung gian cho quan hệ Many-to-Many
/// Liên kết giữa Post và Amenity
/// Mỗi Post có thể có nhiều Amenity
/// Mỗi Amenity có thể thuộc nhiều Post
/// </summary>
public class PostAmenity
{
    /// <summary>
    /// Foreign Key - ID của Post
    /// </summary>
    public int PostId { get; set; }

    /// <summary>
    /// Foreign Key - ID của Amenity
    /// </summary>
    public int AmenityId { get; set; }

    /// <summary>
    /// Ngày thêm tiện ích vào tin đăng
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    /// <summary>
    /// Tin đăng
    /// </summary>
    public virtual Post Post { get; set; } = null!;

    /// <summary>
    /// Tiện ích
    /// </summary>
    public virtual Amenity Amenity { get; set; } = null!;
}
