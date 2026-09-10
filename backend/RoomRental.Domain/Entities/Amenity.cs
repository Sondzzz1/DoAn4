namespace RoomRental.Domain.Entities;

/// <summary>
/// Entity Amenity - Tiện ích phòng
/// Ví dụ: Wifi, Điều hòa, Nóng lạnh, Máy giặt, Chỗ để xe, v.v.
/// Admin quản lý danh sách tiện ích
/// </summary>
public class Amenity
{
    /// <summary>
    /// ID của Amenity - Primary Key
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Tên tiện ích
    /// Ví dụ: "Wifi", "Điều hòa", "Nóng lạnh", "Máy giặt"
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Icon của tiện ích (có thể là class CSS hoặc URL)
    /// Ví dụ: "fa-wifi", "icon-ac", hoặc URL đến icon
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Mô tả về tiện ích
    /// </summary>
    public string? Description { get; set; }

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
    /// Danh sách tin đăng có tiện ích này (Many-to-Many qua PostAmenity)
    /// </summary>
    public virtual ICollection<PostAmenity> PostAmenities { get; set; } = new List<PostAmenity>();
}
