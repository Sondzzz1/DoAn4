using RoomRental.Domain.Enums;

namespace RoomRental.Domain.Entities;

/// <summary>
/// Entity Post - Tin đăng cho thuê phòng
/// Mỗi tin đăng tương ứng với 1 phòng (quan hệ 1-1 với Room)
/// </summary>
public class Post
{
    /// <summary>
    /// ID của Post - Primary Key
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Tiêu đề tin đăng
    /// Ví dụ: "Phòng trọ giá rẻ quận 7, gần chợ"
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Mô tả chi tiết về phòng
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Giá thuê (VNĐ/tháng)
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Foreign Key - ID của Landlord (User)
    /// </summary>
    public int LandlordId { get; set; }

    /// <summary>
    /// Trạng thái tin đăng: Pending, Approved, Rejected, Hidden, Expired
    /// </summary>
    public PostStatus Status { get; set; } = PostStatus.Pending;

    /// <summary>
    /// Lý do từ chối (nếu Status = Rejected)
    /// Admin nhập khi từ chối tin đăng
    /// </summary>
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Ngày tạo tin đăng
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Ngày cập nhật tin đăng lần cuối
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    /// <summary>
    /// Chủ nhà trọ (Landlord) sở hữu tin đăng này
    /// </summary>
    public virtual User Landlord { get; set; } = null!;

    /// <summary>
    /// Thông tin phòng (quan hệ 1-1)
    /// Mỗi Post có 1 Room
    /// </summary>
    public virtual Room Room { get; set; } = null!;

    /// <summary>
    /// Danh sách hình ảnh của tin đăng
    /// </summary>
    public virtual ICollection<PostImage> Images { get; set; } = new List<PostImage>();

    /// <summary>
    /// Danh sách tiện ích của phòng (Many-to-Many qua PostAmenity)
    /// </summary>
    public virtual ICollection<PostAmenity> PostAmenities { get; set; } = new List<PostAmenity>();

    /// <summary>
    /// Danh sách Favorite (người dùng đã thích tin này)
    /// </summary>
    public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();

    /// <summary>
    /// Danh sách lịch hẹn xem phòng cho tin đăng này
    /// </summary>
    public virtual ICollection<ViewingAppointment> Appointments { get; set; } = new List<ViewingAppointment>();
}
