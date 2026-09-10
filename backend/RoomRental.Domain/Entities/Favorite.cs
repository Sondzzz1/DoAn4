namespace RoomRental.Domain.Entities;

/// <summary>
/// Entity Favorite - Phòng yêu thích
/// Tenant có thể lưu các phòng yêu thích để xem lại sau
/// </summary>
public class Favorite
{
    /// <summary>
    /// ID của Favorite - Primary Key
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign Key - ID của User (Tenant)
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Foreign Key - ID của Post
    /// </summary>
    public int PostId { get; set; }

    /// <summary>
    /// Ngày thêm vào danh sách yêu thích
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    /// <summary>
    /// Tenant đã lưu phòng này vào yêu thích
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Tin đăng được yêu thích
    /// </summary>
    public virtual Post Post { get; set; } = null!;
}
