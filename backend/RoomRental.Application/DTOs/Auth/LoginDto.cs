using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

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
    [JsonIgnore]
    public string? Email { get; set; }

    [JsonIgnore]
    public string? Password { get; set; }
    public string? MatKhau { get; set; }

    public string GetEmail() => Email?.Trim() ?? string.Empty;
    public string GetPassword() => !string.IsNullOrWhiteSpace(Password) ? Password : MatKhau ?? string.Empty;
}
