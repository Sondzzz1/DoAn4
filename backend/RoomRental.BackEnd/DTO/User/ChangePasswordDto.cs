using System.ComponentModel.DataAnnotations;

namespace RoomRental.BackEnd.DTO.User;

/// <summary>
/// DTO cho đổi mật khẩu
/// </summary>
public class ChangePasswordDto
{
    public string? CurrentPassword { get; set; }
    public string? MatKhauHienTai { get; set; }

    public string? NewPassword { get; set; }
    public string? MatKhauMoi { get; set; }

    public string? ConfirmPassword { get; set; }
    public string? XacNhanMatKhau { get; set; }

    public string GetCurrentPassword() => !string.IsNullOrWhiteSpace(CurrentPassword) ? CurrentPassword : (MatKhauHienTai ?? string.Empty);
    public string GetNewPassword() => !string.IsNullOrWhiteSpace(NewPassword) ? NewPassword : (MatKhauMoi ?? string.Empty);
    public string GetConfirmPassword() => !string.IsNullOrWhiteSpace(ConfirmPassword) ? ConfirmPassword : (XacNhanMatKhau ?? string.Empty);
}
