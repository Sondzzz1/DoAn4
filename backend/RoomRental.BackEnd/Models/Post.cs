using RoomRental.BackEnd.Models.Enums;

namespace RoomRental.BackEnd.Models;

/// <summary>
/// Entity TinDang - bài đăng hiển thị phòng.
/// </summary>
public class Post
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public int LandlordId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public decimal DisplayPrice { get; set; }
    public PostStatus Status { get; set; } = PostStatus.Pending;
    public string? RejectionReason { get; set; }
    public int ViewCount { get; set; }
    public DateTime? PostedAt { get; set; }
    public DateTime? ExpiredAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    public virtual Room Room { get; set; } = null!;
    public virtual LandlordProfile Landlord { get; set; } = null!;
    public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public virtual ICollection<ViewingAppointment> Appointments { get; set; } = new List<ViewingAppointment>();
    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();
}
