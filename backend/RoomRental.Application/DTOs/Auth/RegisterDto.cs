using System.ComponentModel.DataAnnotations;

namespace RoomRental.Application.DTOs.Auth;

/// <summary>
/// DTO cho đăng ký tài khoản
/// </summary>
public class RegisterDto
{
    /// <summary>
    /// Họ và tên đầy đủ
    /// </summary>
    [Required(ErrorMessage = "Họ tên là bắt buộc")]
    [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Email - Dùng để đăng nhập
    /// </summary>
    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Số điện thoại
    /// </summary>
    [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự")]
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Mật khẩu
    /// </summary>
    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 đến 100 ký tự")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Xác nhận mật khẩu
    /// </summary>
    [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc")]
    [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>
    /// Tên Role: Tenant hoặc Landlord
    /// Không cho đăng ký trực tiếp với role Admin
    /// </summary>
    [Required(ErrorMessage = "Loại tài khoản là bắt buộc")]
    [RegularExpression("^(Tenant|Landlord)$", ErrorMessage = "Loại tài khoản phải là Tenant hoặc Landlord")]
    public string RoleName { get; set; } = string.Empty;
}
