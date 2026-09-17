using Microsoft.AspNetCore.Mvc;
using RoomRental.Application.DTOs.Auth;
using RoomRental.Application.DTOs.Common;
using RoomRental.Application.Interfaces;

namespace RoomRental.API.Controllers;

/// <summary>
/// Controller xử lý Authentication (Đăng ký, Đăng nhập)
/// </summary>
[Route("api/xac-thuc")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Đăng ký tài khoản mới
    /// </summary>
    /// <param name="registerDto">Thông tin đăng ký</param>
    /// <returns>Thông tin user và JWT token</returns>
    [HttpPost("dang-ky")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 400)]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<AuthResponseDto>.ErrorResponse(
                    "Dữ liệu không hợp lệ",
                    errors
                ));
            }

            var result = await _authService.RegisterAsync(registerDto);

            return Ok(ApiResponse<AuthResponseDto>.SuccessResponse(
                result,
                "Đăng ký tài khoản thành công"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi đăng ký tài khoản");
            return BadRequest(ApiResponse<AuthResponseDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Đăng nhập
    /// </summary>
    /// <param name="loginDto">Thông tin đăng nhập</param>
    /// <returns>Thông tin user và JWT token</returns>
    [HttpPost("dang-nhap")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 400)]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<AuthResponseDto>.ErrorResponse(
                    "Dữ liệu không hợp lệ",
                    errors
                ));
            }

            var result = await _authService.LoginAsync(loginDto);

            return Ok(ApiResponse<AuthResponseDto>.SuccessResponse(
                result,
                "Đăng nhập thành công"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi đăng nhập");
            return BadRequest(ApiResponse<AuthResponseDto>.ErrorResponse(ex.Message));
        }
    }
}
