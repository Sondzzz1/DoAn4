using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomRental.BackEnd.BLL.Interfaces;
using RoomRental.BackEnd.DTO.Chat;
using RoomRental.BackEnd.DTO.Common;
using System.Security.Claims;

namespace RoomRental.BackEnd.Controllers;

[Route("api/tin-nhan")]
[ApiController]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IChatService chatService, ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách cuộc trò chuyện của người dùng
    /// </summary>
    [HttpGet("hoi-thoai")]
    public async Task<IActionResult> GetConversations()
    {
        try
        {
            var userId = GetCurrentUserId();
            var conversations = await _chatService.GetConversationsAsync(userId);
            return Ok(ApiResponse<List<ConversationDto>>.SuccessResponse(conversations, "Lấy danh sách hội thoại thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách cuộc trò chuyện");
            return this.BusinessError<List<ConversationDto>>(ex);
        }
    }

    /// <summary>
    /// Lấy lịch sử tin nhắn với một người dùng cụ thể
    /// </summary>
    [HttpGet("hoi-thoai/{partnerId:int}")]
    public async Task<IActionResult> GetMessages(int partnerId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var messages = await _chatService.GetMessagesAsync(userId, partnerId);
            return Ok(ApiResponse<List<ChatMessageDto>>.SuccessResponse(messages, "Lấy tin nhắn thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy tin nhắn với partnerId: {PartnerId}", partnerId);
            return this.BusinessError<List<ChatMessageDto>>(ex);
        }
    }

    /// <summary>
    /// Gửi tin nhắn
    /// </summary>
    [HttpPost("gui")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var message = await _chatService.SendMessageAsync(userId, dto);
            return Ok(ApiResponse<ChatMessageDto>.SuccessResponse(message, "Gửi tin nhắn thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gửi tin nhắn");
            return this.BusinessError<ChatMessageDto>(ex);
        }
    }

    /// <summary>
    /// Đánh dấu đã đọc tin nhắn
    /// </summary>
    [HttpPut("doc/{partnerId:int}")]
    public async Task<IActionResult> MarkAsRead(int partnerId)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _chatService.MarkAsReadAsync(userId, partnerId);
            return Ok(ApiResponse<bool>.SuccessResponse(true, "Đã đánh dấu đã đọc"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi đánh dấu đã đọc");
            return this.BusinessError<bool>(ex);
        }
    }

    /// <summary>
    /// Số tin nhắn chưa đọc
    /// </summary>
    [HttpGet("chua-doc")]
    public async Task<IActionResult> GetUnreadCount()
    {
        try
        {
            var userId = GetCurrentUserId();
            var count = await _chatService.GetUnreadCountAsync(userId);
            return Ok(ApiResponse<int>.SuccessResponse(count, "Lấy số tin chưa đọc thành công"));
        }
        catch (Exception ex)
        {
            return this.BusinessError<int>(ex);
        }
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("UserId")?.Value 
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            throw new Exception("Không thể xác thực người dùng");
        }

        return userId;
    }
}
