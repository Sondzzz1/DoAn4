using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomRental.Application.DTOs.Common;
using RoomRental.Application.DTOs.Room;
using RoomRental.Application.Interfaces;
using RoomRental.Domain.Enums;
using System.Security.Claims;

namespace RoomRental.API.Controllers;

/// <summary>
/// Controller xử lý Quản lý Phòng trọ (Mục 15, 16)
/// </summary>
[Route("api/phong")]
[ApiController]
[Authorize(Roles = "Landlord,Admin")]
public class RoomController : ControllerBase
{
    private readonly IRoomService _roomService;
    private readonly ILogger<RoomController> _logger;

    public RoomController(IRoomService roomService, ILogger<RoomController> logger)
    {
        _roomService = roomService;
        _logger = logger;
    }

    /// <summary>
    /// Landlord xem danh sách phòng thuộc sở hữu (Mục 15: GET /api/rooms)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<RoomDto>>), 200)]
    public async Task<IActionResult> GetLandlordRooms([FromQuery] RoomStatus? status)
    {
        try
        {
            var accountId = GetCurrentUserId();
            var rooms = await _roomService.GetLandlordRoomsAsync(accountId, status);

            return Ok(ApiResponse<List<RoomDto>>.SuccessResponse(rooms, "Lấy danh sách phòng thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách phòng của Landlord");
            return BadRequest(ApiResponse<List<RoomDto>>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Xem chi tiết phòng (GET /api/rooms/{id})
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<RoomDto>), 200)]
    public async Task<IActionResult> GetRoomById(int id)
    {
        try
        {
            var room = await _roomService.GetRoomByIdAsync(id);
            return Ok(ApiResponse<RoomDto>.SuccessResponse(room, "Lấy thông tin phòng thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy thông tin phòng ID: {Id}", id);
            return BadRequest(ApiResponse<RoomDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Thêm phòng mới (POST /api/rooms)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<RoomDto>), 200)]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomDto createDto)
    {
        try
        {
            var accountId = GetCurrentUserId();
            var room = await _roomService.CreateRoomAsync(accountId, createDto);

            return Ok(ApiResponse<RoomDto>.SuccessResponse(room, "Thêm phòng mới thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi thêm phòng mới");
            return BadRequest(ApiResponse<RoomDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Cập nhật thông tin phòng (PUT /api/rooms/{id})
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<RoomDto>), 200)]
    public async Task<IActionResult> UpdateRoom(int id, [FromBody] UpdateRoomDto updateDto)
    {
        try
        {
            var accountId = GetCurrentUserId();
            var room = await _roomService.UpdateRoomAsync(accountId, id, updateDto);

            return Ok(ApiResponse<RoomDto>.SuccessResponse(room, "Cập nhật phòng thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật phòng ID: {Id}", id);
            return BadRequest(ApiResponse<RoomDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Cập nhật trạng thái phòng (Mục 16: PUT /api/rooms/{id}/status)
    /// </summary>
    [HttpPut("{id:int}/trang-thai")]
    [ProducesResponseType(typeof(ApiResponse<RoomDto>), 200)]
    public async Task<IActionResult> UpdateRoomStatus(int id, [FromBody] UpdateRoomStatusDto statusDto)
    {
        try
        {
            var accountId = GetCurrentUserId();
            var status = statusDto.GetResolvedStatus();
            var room = await _roomService.UpdateRoomStatusAsync(accountId, id, status);

            return Ok(ApiResponse<RoomDto>.SuccessResponse(room, $"Cập nhật trạng thái phòng thành công: {status}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật trạng thái phòng ID: {Id}", id);
            return BadRequest(ApiResponse<RoomDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Xóa/Tạm ngưng phòng (DELETE /api/rooms/{id})
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> DeleteRoom(int id)
    {
        try
        {
            var accountId = GetCurrentUserId();
            await _roomService.DeleteRoomAsync(accountId, id);

            return Ok(ApiResponse<object?>.SuccessResponse(null, "Đã xóa phòng thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa phòng ID: {Id}", id);
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
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
