namespace RoomRental.Domain.Enums;

/// <summary>
/// Trạng thái của lịch hẹn xem phòng
/// </summary>
public enum AppointmentStatus
{
    /// <summary>
    /// Đang chờ xác nhận - Người tìm trọ đã gửi yêu cầu, chờ Chủ trọ xác nhận
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Đã được chấp nhận - Chủ trọ đã xác nhận lịch hẹn
    /// </summary>
    Approved = 1,

    /// <summary>
    /// Bị từ chối - Chủ trọ từ chối lịch hẹn
    /// </summary>
    Rejected = 2,

    /// <summary>
    /// Đã hủy - Người tìm trọ hoặc Chủ trọ hủy lịch hẹn
    /// </summary>
    Cancelled = 3,

    /// <summary>
    /// Đã hoàn thành - Đã xem phòng xong
    /// </summary>
    Completed = 4
}
