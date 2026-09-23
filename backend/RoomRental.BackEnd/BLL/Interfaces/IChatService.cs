using RoomRental.BackEnd.DTO.Chat;

namespace RoomRental.BackEnd.BLL.Interfaces;

public interface IChatService
{
    Task<List<ConversationDto>> GetConversationsAsync(int userId);
    Task<List<ChatMessageDto>> GetMessagesAsync(int userId, int partnerId);
    Task<ChatMessageDto> SendMessageAsync(int senderId, SendMessageDto dto);
    Task<bool> MarkAsReadAsync(int userId, int partnerId);
    Task<int> GetUnreadCountAsync(int userId);
}
