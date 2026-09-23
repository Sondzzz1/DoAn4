namespace RoomRental.BackEnd.Models.Enums;

/// <summary>
/// Trạng thái của lịch hẹn xem phòng
/// </summary>
public enum AppointmentStatus
{
    Pending = 0,
    Confirmed = 1,
    Approved = 1,
    Rejected = 2,
    Completed = 3,
    Cancelled = 4
}
