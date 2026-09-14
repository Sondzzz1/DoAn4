using System.ComponentModel.DataAnnotations;

namespace RoomRental.Application.DTOs.User;

/// <summary>
/// DTO cho cập nhật thông tin cá nhân
/// </summary>
public class UpdateProfileDto
{
    [Required(ErrorMessage = "Họ tên là bắt buộc")]
    [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự")]
    public string Phone { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "URL ảnh không được vượt quá 500 ký tự")]
    public string? AvatarUrl { get; set; }
}
