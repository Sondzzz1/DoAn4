using RoomRental.BackEnd.Models.Enums;

namespace RoomRental.BackEnd.DTO.ViewingAppointment;

public class AppointmentDto
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public string PostTitle { get; set; } = string.Empty;
    public string PostAddress { get; set; } = string.Empty;
    public decimal PostPrice { get; set; }
    public string? RoomName { get; set; }

    public int TenantId { get; set; }
    public int TenantAccountId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string TenantPhone { get; set; } = string.Empty;
    public string? TenantAvatar { get; set; }

    public int LandlordId { get; set; }
    public int LandlordAccountId { get; set; }
    public string LandlordName { get; set; } = string.Empty;
    public string LandlordPhone { get; set; } = string.Empty;

    public DateTime ScheduledAt { get; set; }
    public string NgayXem => ScheduledAt.ToString("yyyy-MM-dd");
    public string GioXem => ScheduledAt.ToString("HH:mm");

    public AppointmentStatus Status { get; set; }
    public string StatusText => Status.ToString();

    public string? TenantNote { get; set; }
    public string? LandlordResponse { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
