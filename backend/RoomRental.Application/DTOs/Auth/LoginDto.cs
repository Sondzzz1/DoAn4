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
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string? Email { get; set; }

    public string? Password { get; set; }
    public string? MatKhau { get; set; }

    public string GetEmail() => Email?.Trim() ?? string.Empty;
    public string GetPassword() => !string.IsNullOrWhiteSpace(Password) ? Password : MatKhau ?? string.Empty;
}
