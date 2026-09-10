namespace RoomRental.Domain.Entities;

/// <summary>
/// Entity PostImage - Hình ảnh của tin đăng
/// Mỗi tin đăng có thể có nhiều hình ảnh
/// </summary>
public class PostImage
{
    /// <summary>
    /// ID của PostImage - Primary Key
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign Key - ID của Post
    /// </summary>
    public int PostId { get; set; }

    /// <summary>
    /// URL của hình ảnh
    /// Có thể lưu trên server hoặc cloud (Cloudinary, AWS S3, etc.)
    /// </summary>
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>
    /// Thứ tự hiển thị của ảnh
    /// Ảnh có DisplayOrder thấp nhất sẽ là ảnh đại diện
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// Ngày upload ảnh
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    /// <summary>
    /// Tin đăng sở hữu hình ảnh này
    /// </summary>
    public virtual Post Post { get; set; } = null!;
}
