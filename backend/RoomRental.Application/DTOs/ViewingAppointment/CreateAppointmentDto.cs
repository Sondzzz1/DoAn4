namespace RoomRental.Application.DTOs.ViewingAppointment;

using System.Text.Json.Serialization;

/// <summary>
/// DTO đặt lịch xem phòng từ Tenant
/// Hỗ trợ cả property tiếng Anh và tiếng Việt (Mục 7)
/// </summary>
public class CreateAppointmentDto
{
    [JsonIgnore] public int PostId { get; set; }
    public int BaiDangId { get => PostId; set => PostId = value; }
    [JsonIgnore] public int? LandlordId { get; set; }
    public int? ChuTroId { get => LandlordId; set => LandlordId = value; }

    public string? NgayXem { get; set; }
    public string? GioXem { get; set; }
    [JsonIgnore] public DateTime? ScheduledAt { get; set; }

    public string? GhiChu { get; set; }
    [JsonIgnore] public string? TenantNote { get; set; }

    public DateTime GetScheduledDateTime()
    {
        if (ScheduledAt.HasValue) return ScheduledAt.Value;

        if (!string.IsNullOrWhiteSpace(NgayXem))
        {
            var dateStr = NgayXem.Trim();
            var timeStr = !string.IsNullOrWhiteSpace(GioXem) ? GioXem.Trim() : "09:00";
            if (DateTime.TryParse($"{dateStr} {timeStr}", out var dt))
            {
                return dt;
            }
        }

        return DateTime.UtcNow.AddDays(1);
    }

    public string? GetNote() => !string.IsNullOrWhiteSpace(TenantNote) ? TenantNote : GhiChu;
}
