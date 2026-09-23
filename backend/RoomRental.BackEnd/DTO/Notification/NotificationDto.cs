namespace RoomRental.BackEnd.DTO.Notification;

public class NotificationDto
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int? Type { get; set; }
    public string? Link { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
