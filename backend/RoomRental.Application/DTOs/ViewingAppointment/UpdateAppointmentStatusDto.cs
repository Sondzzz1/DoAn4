namespace RoomRental.Application.DTOs.ViewingAppointment;

using System.Text.Json.Serialization;

public class UpdateAppointmentStatusDto
{
    [JsonIgnore] public string? Reason { get; set; }
    public string? LyDo { get; set; }
    [JsonIgnore] public string? Response { get; set; }
    public string? PhanHoi { get => Response; set => Response = value; }

    public string? GetReason() => !string.IsNullOrWhiteSpace(Response) ? Response : (!string.IsNullOrWhiteSpace(LyDo) ? LyDo : Reason);
}
