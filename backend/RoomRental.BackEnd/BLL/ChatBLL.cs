using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RoomRental.BackEnd.BLL.Interfaces;
using RoomRental.BackEnd.DAL;
using RoomRental.BackEnd.DTO.Chat;
using RoomRental.BackEnd.Hubs;
using RoomRental.BackEnd.Models;

namespace RoomRental.BackEnd.BLL;

public class ChatBLL : IChatService
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<ChatHub> _chatHubContext;
    private readonly INotificationService _notificationService;

    public ChatBLL(
        ApplicationDbContext context,
        IHubContext<ChatHub> chatHubContext,
        INotificationService notificationService)
    {
        _context = context;
        _chatHubContext = chatHubContext;
        _notificationService = notificationService;
    }

    public async Task<List<ConversationDto>> GetConversationsAsync(int userId)
    {
        var messages = await _context.ChatMessages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .Include(m => m.Post)
            .Where(m => m.SenderId == userId || m.ReceiverId == userId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

        var partnerIds = messages
            .Select(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
            .Distinct()
            .ToList();

        var users = await _context.Users
            .Where(u => partnerIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);

        var conversations = new List<ConversationDto>();

        foreach (var pId in partnerIds)
        {
            if (!users.TryGetValue(pId, out var partner)) continue;

            var lastMsg = messages.First(m => (m.SenderId == userId && m.ReceiverId == pId) || (m.SenderId == pId && m.ReceiverId == userId));
            var unread = messages.Count(m => m.SenderId == pId && m.ReceiverId == userId && !m.IsRead);

            conversations.Add(new ConversationDto
            {
                PartnerId = pId,
                PartnerName = partner.FullName,
                PartnerAvatar = partner.AvatarUrl,
                PartnerRole = partner.RoleName,
                LastMessage = lastMsg.Message,
                LastMessageTime = lastMsg.CreatedAt,
                UnreadCount = unread,
                PostId = lastMsg.PostId,
                PostTitle = lastMsg.Post?.Title
            });
        }

        return conversations.OrderByDescending(c => c.LastMessageTime).ToList();
    }

    public async Task<List<ChatMessageDto>> GetMessagesAsync(int userId, int partnerId)
    {
        var messages = await _context.ChatMessages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .Include(m => m.Post)
                .ThenInclude(p => p!.Room)
                    .ThenInclude(r => r.Images)
            .Where(m => (m.SenderId == userId && m.ReceiverId == partnerId) || (m.SenderId == partnerId && m.ReceiverId == userId))
            .OrderBy(m => m.CreatedAt)
            .Select(m => new ChatMessageDto
            {
                Id = m.Id,
                SenderId = m.SenderId,
                SenderName = m.Sender.FullName,
                SenderAvatar = m.Sender.AvatarUrl,
                ReceiverId = m.ReceiverId,
                ReceiverName = m.Receiver.FullName,
                ReceiverAvatar = m.Receiver.AvatarUrl,
                PostId = m.PostId,
                PostTitle = m.Post != null ? m.Post.Title : null,
                PostPrice = m.Post != null ? m.Post.DisplayPrice : null,
                PostImage = m.Post != null && m.Post.Room.Images.Any() ? m.Post.Room.Images.First().ImageUrl : null,
                Message = m.Message,
                IsRead = m.IsRead,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync();

        // Tự động đánh dấu đã đọc
        var unreadMessages = await _context.ChatMessages
            .Where(m => m.SenderId == partnerId && m.ReceiverId == userId && !m.IsRead)
            .ToListAsync();

        if (unreadMessages.Any())
        {
            foreach (var msg in unreadMessages)
            {
                msg.IsRead = true;
            }
            await _context.SaveChangesAsync();
        }

        return messages;
    }

    public async Task<ChatMessageDto> SendMessageAsync(int senderId, SendMessageDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Message))
        {
            throw new Exception("Nội dung tin nhắn không được để trống.");
        }

        var receiver = await _context.Users.FindAsync(dto.ReceiverId)
            ?? throw new Exception("Không tìm thấy người nhận tin nhắn.");

        var sender = await _context.Users.FindAsync(senderId)
            ?? throw new Exception("Không tìm thấy thông tin người gửi.");

        var msg = new ChatMessage
        {
            SenderId = senderId,
            ReceiverId = dto.ReceiverId,
            PostId = dto.PostId,
            Message = dto.Message.Trim(),
            IsRead = false,
            CreatedAt = DateTime.Now
        };

        _context.ChatMessages.Add(msg);
        await _context.SaveChangesAsync();

        Post? post = null;
        if (dto.PostId.HasValue)
        {
            post = await _context.Posts
                .Include(p => p.Room)
                    .ThenInclude(r => r.Images)
                .FirstOrDefaultAsync(p => p.Id == dto.PostId.Value);
        }

        var resultDto = new ChatMessageDto
        {
            Id = msg.Id,
            SenderId = sender.Id,
            SenderName = sender.FullName,
            SenderAvatar = sender.AvatarUrl,
            ReceiverId = receiver.Id,
            ReceiverName = receiver.FullName,
            ReceiverAvatar = receiver.AvatarUrl,
            PostId = msg.PostId,
            PostTitle = post?.Title,
            PostPrice = post?.DisplayPrice,
            PostImage = post?.Room.Images.FirstOrDefault()?.ImageUrl,
            Message = msg.Message,
            IsRead = false,
            CreatedAt = msg.CreatedAt
        };

        // Bắn SignalR event cho người nhận và người gửi
        var receiverConnections = ChatHub.GetConnections(dto.ReceiverId);
        if (receiverConnections.Any())
        {
            await _chatHubContext.Clients.Clients(receiverConnections).SendAsync("ReceiveMessage", resultDto);
        }

        var senderConnections = ChatHub.GetConnections(senderId);
        if (senderConnections.Any())
        {
            await _chatHubContext.Clients.Clients(senderConnections).SendAsync("MessageSent", resultDto);
        }

        return resultDto;
    }

    public async Task<bool> MarkAsReadAsync(int userId, int partnerId)
    {
        var unread = await _context.ChatMessages
            .Where(m => m.SenderId == partnerId && m.ReceiverId == userId && !m.IsRead)
            .ToListAsync();

        if (unread.Any())
        {
            foreach (var m in unread) m.IsRead = true;
            await _context.SaveChangesAsync();
        }
        return true;
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _context.ChatMessages.CountAsync(m => m.ReceiverId == userId && !m.IsRead);
    }
}
