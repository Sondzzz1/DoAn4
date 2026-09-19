using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RoomRental.Application.DTOs.Auth;

/// <summary>
/// DTO cho đăng ký tài khoản
/// </summary>
public class RegisterDto
{
    [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
    [JsonIgnore]
    public string? FullName { get; set; }

    [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
    public string? HoTen { get; set; }

    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự")]
    public string? Email { get; set; }

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự")]
    [JsonIgnore]
    public string? Phone { get; set; }

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự")]
    public string? SoDienThoai { get; set; }

    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 đến 100 ký tự")]
    [JsonIgnore]
    public string? Password { get; set; }

    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 đến 100 ký tự")]
    public string? MatKhau { get; set; }

    [JsonIgnore]
    public string? ConfirmPassword { get; set; }
    public string? XacNhanMatKhau { get => ConfirmPassword; set => ConfirmPassword = value; }

    [JsonIgnore]
    public string? RoleName { get; set; }
    public string? VaiTro { get; set; }

    public string GetFullName() => FirstNonEmpty(FullName, HoTen);
    public string GetPhone() => FirstNonEmpty(Phone, SoDienThoai);
    public string GetPassword() => FirstNonEmpty(Password, MatKhau);
    public string GetConfirmPassword() => FirstNonEmpty(ConfirmPassword, XacNhanMatKhau);
    public string GetRoleName() => FirstNonEmpty(VaiTro, RoleName, "Tenant");

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim() ?? string.Empty;
    }
}
