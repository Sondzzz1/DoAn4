using System.ComponentModel.DataAnnotations;

namespace RoomRental.Application.DTOs.User;

/// <summary>
/// DTO cho cập nhật thông tin cá nhân
/// </summary>
public class UpdateProfileDto
{
    [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
    public string? FullName { get; set; }
    public string? HoTen { get; set; }

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự")]
    public string? Phone { get; set; }
    public string? SoDienThoai { get; set; }

    [StringLength(500, ErrorMessage = "URL ảnh không được vượt quá 500 ký tự")]
    public string? AvatarUrl { get; set; }
    public string? AnhDaiDien { get; set; }
    public string? Address { get; set; }
    public string? DiaChi { get; set; }
    public string? Introduction { get; set; }
    public string? GioiThieu { get; set; }

    public string? GetFullName() => FirstNonEmpty(FullName, HoTen);
    public string? GetPhone() => FirstNonEmpty(Phone, SoDienThoai);
    public string? GetAvatarUrl() => FirstNonEmpty(AvatarUrl, AnhDaiDien);
    public string? GetAddress() => FirstNonEmpty(Address, DiaChi);
    public string? GetIntroduction() => FirstNonEmpty(Introduction, GioiThieu);

    private static string? FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();
    }
}
