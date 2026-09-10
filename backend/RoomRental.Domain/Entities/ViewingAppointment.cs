using RoomRental.Domain.Enums;

namespace RoomRental.Domain.Entities;

/// <summary>
/// Entity ViewingAppointment - Lịch hẹn xem phòng
/// Tenant đặt lịch để xem phòng
/// Landlord xác nhận hoặc từ chối
/// </summary>
public class ViewingAppointment
{
    /// <summary>
    /// ID của ViewingAppointment - Primary Key
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign Key - ID của Tenant (User)
    /// </summary>
    public int TenantId { get; set; }

    /// <summary>
    /// Foreign Key - ID của Post
    /// </summary>
    public int PostId { get; set; }

    /// <summary>
    /// Ngày và giờ hẹn xem phòng
    /// </summary>
    public DateTime ScheduledAt { get; set; }

    /// <summary>
    /// Trạng thái lịch hẹn: Pending, Approved, Rejected, Cancelled, Completed
    /// </summary>
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

    /// <summary>
    /// Ghi chú của Tenant khi đặt lịch
    /// Ví dụ: "Tôi muốn xem phòng vào buổi chiều"
    /// </summary>
    public string? TenantNote { get; set; }

    /// <summary>
    /// Phản hồi của Landlord
    /// Nếu Status = Rejected, Landlord có thể ghi lý do
    /// Nếu Status = Approved, Landlord có thể ghi ghi chú
    /// </summary>
    public string? LandlordResponse { get; set; }

    /// <summary>
    /// Ngày tạo lịch hẹn
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Ngày cập nhật trạng thái lần cuối
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    /// <summary>
    /// Tenant đã đặt lịch hẹn này
    /// </summary>
    public virtual User Tenant { get; set; } = null!;

    /// <summary>
    /// Tin đăng được đặt lịch xem
    /// </summary>
    public virtual Post Post { get; set; } = null!;
}
