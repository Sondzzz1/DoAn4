using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomRental.BackEnd.BLL.Interfaces;
using RoomRental.BackEnd.DTO.Common;
using RoomRental.BackEnd.DTO.Notification;
using System.Security.Claims;

namespace RoomRental.BackEnd.Controllers;

[Route("api/thong-bao")]
[ApiController]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(INotificationService notificationService, ILogger<NotificationController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách thông báo của người dùng
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMyNotifications()
    {
        try
        {
            var userId = GetCurrentUserId();
            var notifications = await _notificationService.GetMyNotificationsAsync(userId);
            return Ok(ApiResponse<List<NotificationDto>>.SuccessResponse(notifications, "Lấy danh sách thông báo thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy thông báo");
            return BadRequest(ApiResponse<List<NotificationDto>>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Lấy số lượng thông báo chưa đọc
    /// </summary>
    [HttpGet("chua-doc")]
    public async Task<IActionResult> GetUnreadCount()
    {
        try
        {
            var userId = GetCurrentUserId();
            var count = await _notificationService.GetUnreadCountAsync(userId);
            return Ok(ApiResponse<int>.SuccessResponse(count, "Lấy số lượng thông báo chưa đọc thành công"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<int>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Đánh dấu 1 thông báo là đã đọc
    /// </summary>
    [HttpPut("{id:int}/da-doc")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _notificationService.MarkAsReadAsync(userId, id);
            return Ok(ApiResponse<bool>.SuccessResponse(result, "Đã đánh dấu thông báo là đã đọc"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi đánh dấu đã đọc thông báo ID: {Id}", id);
            return BadRequest(ApiResponse<bool>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Đánh dấu tất cả thông báo là đã đọc
    /// </summary>
    [HttpPut("doc-tat-ca")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _notificationService.MarkAllAsReadAsync(userId);
            return Ok(ApiResponse<bool>.SuccessResponse(result, "Đã đánh dấu tất cả thông báo là đã đọc"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi đánh dấu đã đọc tất cả thông báo");
            return BadRequest(ApiResponse<bool>.ErrorResponse(ex.Message));
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
