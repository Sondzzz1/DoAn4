namespace RoomRental.Domain.Entities;

/// <summary>
/// Entity ThongBao.
/// </summary>
public class Notification
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int? Type { get; set; }
    public string? Link { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public virtual User Account { get; set; } = null!;
}
