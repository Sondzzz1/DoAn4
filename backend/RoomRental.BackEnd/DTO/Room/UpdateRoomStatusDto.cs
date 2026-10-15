using RoomRental.BackEnd.BLL;
using RoomRental.BackEnd.Models.Enums;

namespace RoomRental.BackEnd.DTO.Room;

public class UpdateRoomStatusDto
{
    public string? Status { get; set; }
    public RoomStatus? DesiredStatus { get; set; }
    public string? TrangThai { get; set; }

    public RoomStatus GetResolvedStatus()
    {
        RoomStatus? resolved = null;
        if (DesiredStatus.HasValue)
        {
            if (!Enum.IsDefined(DesiredStatus.Value))
                throw new BusinessRuleException("Trạng thái phòng không hợp lệ.");
            resolved = DesiredStatus.Value;
        }

        foreach (var raw in new[] { Status, TrangThai })
        {
            if (raw == null) continue;
            var parsed = raw.Trim().ToLowerInvariant() switch
            {
                "available" or "controng" or "còn trống" or "0" => RoomStatus.Available,
                "rented" or "dathue" or "đã thuê" or "1" => RoomStatus.Rented,
                "reserved" or "giucho" or "đang giữ chỗ" or "đã giữ chỗ" or "2" => RoomStatus.Reserved,
                "maintenance" or "tamngung" or "tạm ngưng" or "temporarilyunavailable" or "3" => RoomStatus.TemporarilyUnavailable,
                _ => throw new BusinessRuleException("Trạng thái phòng không hợp lệ.")
            };
            if (resolved.HasValue && resolved.Value != parsed)
                throw new BusinessRuleException("Các trường trạng thái phòng không khớp nhau.");
            resolved = parsed;
        }

        return resolved ?? throw new BusinessRuleException("Vui lòng cung cấp trạng thái phòng.");
    }
}
