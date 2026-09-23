using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomRental.BackEnd.DTO.Common;
using RoomRental.BackEnd.DTO.User;
using RoomRental.BackEnd.BLL.Interfaces;
using System.Security.Claims;

namespace RoomRental.BackEnd.Controllers;

/// <summary>
/// Controller xử lý User Profile (Mục 9: Quản lý thông tin cá nhân)
/// </summary>
[Route("api/nguoi-dung")]
[ApiController]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserService userService, ILogger<UserController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Lấy thông tin profile của user hiện tại (Mục 9: GET /api/users/me)
    /// </summary>
    [HttpGet("thong-tin")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            var userId = GetCurrentUserId();
            var user = await _userService.GetProfileAsync(userId);

            return Ok(ApiResponse<UserDto>.SuccessResponse(user, "Lấy thông tin thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy thông tin profile");
            return BadRequest(ApiResponse<UserDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Cập nhật thông tin profile (Mục 9: PUT /api/users/me)
    /// </summary>
    [HttpPut("thong-tin")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto updateDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<UserDto>.ErrorResponse("Dữ liệu không hợp lệ", errors));
            }

            var userId = GetCurrentUserId();
            var user = await _userService.UpdateProfileAsync(userId, updateDto);

            return Ok(ApiResponse<UserDto>.SuccessResponse(user, "Cập nhật thông tin thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật profile");
            return BadRequest(ApiResponse<UserDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Đổi mật khẩu
    /// </summary>
    [HttpPost("doi-mat-khau")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<object>.ErrorResponse("Dữ liệu không hợp lệ", errors));
            }

            var userId = GetCurrentUserId();
            await _userService.ChangePasswordAsync(userId, changePasswordDto);

            return Ok(ApiResponse<object?>.SuccessResponse(null, "Đổi mật khẩu thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi đổi mật khẩu");
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
