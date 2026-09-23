namespace RoomRental.BackEnd.DTO.Room;

using System.Text.Json.Serialization;

public class UpdateRoomStatusDto
{
    [JsonIgnore] public string? Status { get; set; }
    [JsonIgnore] public RoomRental.BackEnd.Models.Enums.RoomStatus? DesiredStatus { get; set; }
    public string? TrangThai { get => Status; set => Status = value; }

    public RoomRental.BackEnd.Models.Enums.RoomStatus GetResolvedStatus()
    {
        if (DesiredStatus.HasValue) return DesiredStatus.Value;

        if (!string.IsNullOrWhiteSpace(Status))
        {
            var s = Status.Trim().ToLowerInvariant();
            return s switch
            {
                "available" or "controng" or "còn trống" or "0" => RoomRental.BackEnd.Models.Enums.RoomStatus.Available,
                "rented" or "dathue" or "đã thuê" or "1" => RoomRental.BackEnd.Models.Enums.RoomStatus.Rented,
                "maintenance" or "tamngung" or "tạm ngưng" or "temporarilyunavailable" or "2" => RoomRental.BackEnd.Models.Enums.RoomStatus.TemporarilyUnavailable,
                _ => RoomRental.BackEnd.Models.Enums.RoomStatus.Available
            };
        }

        return RoomRental.BackEnd.Models.Enums.RoomStatus.Available;
    }
}
