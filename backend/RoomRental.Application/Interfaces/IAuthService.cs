using RoomRental.Application.DTOs.Auth;

namespace RoomRental.Application.Interfaces;

/// <summary>
/// Interface cho Authentication Service
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Đăng ký tài khoản mới
    /// </summary>
    Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);

    /// <summary>
    /// Đăng nhập
    /// </summary>
    Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
}
