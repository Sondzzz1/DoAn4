namespace RoomRental.Application.DTOs.Room;

public class UpdateRoomStatusDto
{
    public string? Status { get; set; }
    public RoomRental.Domain.Enums.RoomStatus? DesiredStatus { get; set; }

    public RoomRental.Domain.Enums.RoomStatus GetResolvedStatus()
    {
        if (DesiredStatus.HasValue) return DesiredStatus.Value;

        if (!string.IsNullOrWhiteSpace(Status))
        {
            var s = Status.Trim().ToLowerInvariant();
            return s switch
            {
                "available" or "controng" or "còn trống" or "0" => RoomRental.Domain.Enums.RoomStatus.Available,
                "rented" or "dathue" or "đã thuê" or "1" => RoomRental.Domain.Enums.RoomStatus.Rented,
                "maintenance" or "tamngung" or "tạm ngưng" or "temporarilyunavailable" or "2" => RoomRental.Domain.Enums.RoomStatus.TemporarilyUnavailable,
                _ => RoomRental.Domain.Enums.RoomStatus.Available
            };
        }

        return RoomRental.Domain.Enums.RoomStatus.Available;
    }
}
