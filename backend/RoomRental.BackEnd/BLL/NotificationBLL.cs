using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RoomRental.BackEnd.BLL.Interfaces;
using RoomRental.BackEnd.DAL;
using RoomRental.BackEnd.DTO.Notification;
using RoomRental.BackEnd.Hubs;
using RoomRental.BackEnd.Models;

namespace RoomRental.BackEnd.BLL;

public class NotificationBLL : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationBLL(
        ApplicationDbContext context,
        IHubContext<NotificationHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    public async Task<List<NotificationDto>> GetMyNotificationsAsync(int userId)
    {
        return await _context.Notifications
            .Where(n => n.AccountId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                AccountId = n.AccountId,
                Title = n.Title,
                Content = n.Content,
                Type = n.Type,
                Link = n.Link,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<NotificationDto> CreateNotificationAsync(int userId, string title, string content, int? type = 0, string? link = null)
    {
        var notif = new Notification
        {
            AccountId = userId,
            Title = title,
            Content = content,
            Type = type,
            Link = link,
            IsRead = false,
            CreatedAt = DateTime.Now
        };

        _context.Notifications.Add(notif);
        await _context.SaveChangesAsync();

        var dto = new NotificationDto
        {
            Id = notif.Id,
            AccountId = notif.AccountId,
            Title = notif.Title,
            Content = notif.Content,
            Type = notif.Type,
            Link = notif.Link,
            IsRead = notif.IsRead,
            CreatedAt = notif.CreatedAt
        };

        // Bắn SignalR event cho user
        var connections = NotificationHub.GetConnections(userId);
        if (connections.Any())
        {
            await _hubContext.Clients.Clients(connections).SendAsync("ReceiveNotification", dto);
        }

        return dto;
    }

    public async Task<bool> MarkAsReadAsync(int userId, int notificationId)
    {
        var notif = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.AccountId == userId);

        if (notif != null)
        {
            notif.IsRead = true;
            await _context.SaveChangesAsync();
            return true;
        }
        return false;
    }

    public async Task<bool> MarkAllAsReadAsync(int userId)
    {
        var unread = await _context.Notifications
            .Where(n => n.AccountId == userId && !n.IsRead)
            .ToListAsync();

        if (unread.Any())
        {
            foreach (var n in unread) n.IsRead = true;
            await _context.SaveChangesAsync();
        }
        return true;
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _context.Notifications.CountAsync(n => n.AccountId == userId && !n.IsRead);
    }
}
