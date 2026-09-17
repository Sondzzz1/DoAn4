using RoomRental.Domain.Enums;

namespace RoomRental.Domain.Entities;

/// <summary>
/// Entity LichHenXemPhong.
/// </summary>
public class ViewingAppointment
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int PostId { get; set; }
    public int LandlordId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    public string? TenantNote { get; set; }
    public string? LandlordResponse { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    public virtual TenantProfile Tenant { get; set; } = null!;
    public virtual LandlordProfile Landlord { get; set; } = null!;
    public virtual Post Post { get; set; } = null!;
}
