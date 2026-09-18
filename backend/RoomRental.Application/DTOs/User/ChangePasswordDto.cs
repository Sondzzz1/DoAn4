using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RoomRental.Application.DTOs.User;

/// <summary>
/// DTO cho đổi mật khẩu
/// </summary>
public class ChangePasswordDto
{
    [Required(ErrorMessage = "Mật khẩu hiện tại là bắt buộc")]
    [JsonIgnore] public string CurrentPassword { get; set; } = string.Empty;
    public string MatKhauHienTai { get => CurrentPassword; set => CurrentPassword = value; }

    [Required(ErrorMessage = "Mật khẩu mới là bắt buộc")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu mới phải từ 6 đến 100 ký tự")]
    [JsonIgnore] public string NewPassword { get; set; } = string.Empty;
    public string MatKhauMoi { get => NewPassword; set => NewPassword = value; }

    [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc")]
    [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
    [JsonIgnore] public string ConfirmPassword { get; set; } = string.Empty;
    public string XacNhanMatKhau { get => ConfirmPassword; set => ConfirmPassword = value; }
}
