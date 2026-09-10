namespace RoomRental.Domain.Enums;

/// <summary>
/// Trạng thái của phòng trọ
/// </summary>
public enum RoomStatus
{
    /// <summary>
    /// Còn trống - Phòng sẵn sàng cho thuê
    /// </summary>
    Available = 0,

    /// <summary>
    /// Đã cho thuê - Phòng đã có người thuê
    /// </summary>
    Rented = 1,

    /// <summary>
    /// Tạm thời không khả dụng - Phòng đang sửa chữa hoặc không cho thuê tạm thời
    /// </summary>
    TemporarilyUnavailable = 2
}
