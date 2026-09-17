namespace RoomRental.Application.DTOs.ViewingAppointment;

public class UpdateAppointmentStatusDto
{
    public string? Reason { get; set; }
    public string? LyDo { get; set; }
    public string? Response { get; set; }

    public string? GetReason() => !string.IsNullOrWhiteSpace(Response) ? Response : (!string.IsNullOrWhiteSpace(LyDo) ? LyDo : Reason);
}
