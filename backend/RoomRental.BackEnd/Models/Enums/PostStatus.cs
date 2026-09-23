namespace RoomRental.BackEnd.Models.Enums;

/// <summary>
/// Trạng thái của tin đăng
/// </summary>
public enum PostStatus
{
    /// <summary>
    /// Đang chờ duyệt - Tin mới đăng, chờ Admin duyệt
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Đã được duyệt - Admin đã duyệt, tin được hiển thị công khai
    /// </summary>
    Approved = 1,

    /// <summary>
    /// Bị từ chối - Admin từ chối tin đăng
    /// </summary>
    Rejected = 2,

    /// <summary>
    /// Đã ẩn - Chủ trọ tự ẩn tin hoặc Admin ẩn tin
    /// </summary>
    Hidden = 3,

    /// <summary>
    /// Hết hạn - Tin đăng đã hết thời gian hiển thị
    /// </summary>
    Expired = 4
}
