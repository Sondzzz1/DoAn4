using System.ComponentModel.DataAnnotations;

namespace RoomRental.Application.DTOs.Auth;

/// <summary>
/// DTO cho đăng nhập
/// </summary>
public class LoginDto
{
    /// <summary>
    /// Email
    /// </summary>
    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Mật khẩu
    /// </summary>
    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    public string Password { get; set; } = string.Empty;
}
