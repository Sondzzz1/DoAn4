using System.ComponentModel.DataAnnotations.Schema;

namespace RoomRental.BackEnd.Models;

/// <summary>
/// Entity lưu trữ tin nhắn chat thời gian thực giữa người dùng
/// </summary>
public class ChatMessage
{
    public int Id { get; set; }
    public int SenderId { get; set; }
    public int ReceiverId { get; set; }
    public int? PostId { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public virtual User Sender { get; set; } = null!;
    public virtual User Receiver { get; set; } = null!;
    public virtual Post? Post { get; set; }
}
