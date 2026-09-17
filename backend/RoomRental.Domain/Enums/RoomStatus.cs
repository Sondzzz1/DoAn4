namespace RoomRental.Domain.Enums;

/// <summary>
/// Trạng thái của phòng trọ
/// </summary>
public enum RoomStatus
{
    Available = 0,
    Rented = 1,
    Reserved = 2,
    TemporarilyUnavailable = 3
}
