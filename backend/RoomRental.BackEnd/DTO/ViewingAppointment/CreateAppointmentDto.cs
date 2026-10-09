using System.Globalization;
using RoomRental.BackEnd.BLL;

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

        if (!string.IsNullOrWhiteSpace(NgayXem) && !string.IsNullOrWhiteSpace(GioXem))
        {
            var dateStr = NgayXem.Trim();
            var timeStr = GioXem.Trim();
            if (DateTime.TryParseExact($"{dateStr} {timeStr}", new[] { "yyyy-MM-dd HH:mm", "yyyy-MM-dd HH:mm:ss" },
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                return dt;
            }
        }

        throw new BusinessRuleException("Cần chọn ngày và giờ xem phòng hợp lệ.");
    }

    public string? GetNote() => !string.IsNullOrWhiteSpace(TenantNote) ? TenantNote : GhiChu;
}
