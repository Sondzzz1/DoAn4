namespace RoomRental.BackEnd.DTO.ViewingAppointment;

/// <summary>
/// DTO đặt lịch xem phòng từ Tenant
/// Hỗ trợ cả property tiếng Anh và tiếng Việt
/// </summary>
public class CreateAppointmentDto
{
    public int PostId { get; set; }
    public int BaiDangId { get => PostId; set => PostId = value; }

    public int? LandlordId { get; set; }
    public int? ChuTroId { get => LandlordId; set => LandlordId = value; }

    public string? NgayXem { get; set; }
    public string? GioXem { get; set; }
    public DateTime? ScheduledAt { get; set; }

    public string? GhiChu { get; set; }
    public string? TenantNote { get; set; }

    public int GetPostId() => PostId > 0 ? PostId : BaiDangId;
    public int? GetLandlordId() => LandlordId ?? ChuTroId;

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
