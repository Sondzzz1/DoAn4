using RoomRental.BackEnd.DTO.Notification;

namespace RoomRental.BackEnd.BLL.Interfaces;

public interface INotificationService
{
    Task<List<NotificationDto>> GetMyNotificationsAsync(int userId);
    Task<NotificationDto> CreateNotificationAsync(int userId, string title, string content, int? type = 0, string? link = null);
    Task<bool> MarkAsReadAsync(int userId, int notificationId);
    Task<bool> MarkAllAsReadAsync(int userId);
    Task<int> GetUnreadCountAsync(int userId);
}
