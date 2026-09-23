namespace RoomRental.BackEnd.DTO.Chat;

public class SendMessageDto
{
    public int ReceiverId { get; set; }
    public int? PostId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class ChatMessageDto
{
    public int Id { get; set; }
    public int SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string? SenderAvatar { get; set; }
    public int ReceiverId { get; set; }
    public string ReceiverName { get; set; } = string.Empty;
    public string? ReceiverAvatar { get; set; }
    public int? PostId { get; set; }
    public string? PostTitle { get; set; }
    public decimal? PostPrice { get; set; }
    public string? PostImage { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ConversationDto
{
    public int PartnerId { get; set; }
    public string PartnerName { get; set; } = string.Empty;
    public string? PartnerAvatar { get; set; }
    public string PartnerRole { get; set; } = string.Empty;
    public string LastMessage { get; set; } = string.Empty;
    public DateTime LastMessageTime { get; set; }
    public int UnreadCount { get; set; }
    public int? PostId { get; set; }
    public string? PostTitle { get; set; }
}
