namespace RoomRental.BackEnd.DTO.ViewingAppointment;

public class UpdateAppointmentStatusDto
{
    public string? Reason { get; set; }
    public string? LyDo { get; set; }
    public string? Response { get; set; }
    public string? PhanHoi { get; set; }

    public string? GetReason()
    {
        if (!string.IsNullOrWhiteSpace(Response)) return Response;
        if (!string.IsNullOrWhiteSpace(PhanHoi)) return PhanHoi;
        if (!string.IsNullOrWhiteSpace(LyDo)) return LyDo;
        if (!string.IsNullOrWhiteSpace(Reason)) return Reason;
        return null;
    }
}
