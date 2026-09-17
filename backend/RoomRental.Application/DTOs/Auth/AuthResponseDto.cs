namespace RoomRental.Application.DTOs.Auth;

/// <summary>
/// DTO cho response sau khi đăng nhập/đăng ký thành công
/// </summary>
public class AuthResponseDto
{
    /// <summary>
    /// JWT Token
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// ID của User
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Họ tên
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Email
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Role: Tenant, Landlord, Admin
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Ảnh đại diện
    /// </summary>
    public string? AvatarUrl { get; set; }

    public AuthUserDto User => new()
    {
        Id = UserId,
        HoTen = FullName,
        FullName = FullName,
        Email = Email,
        Role = Role,
        AnhDaiDien = AvatarUrl,
        AvatarUrl = AvatarUrl
    };
}

public class AuthUserDto
{
    public int Id { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? AnhDaiDien { get; set; }
    public string? AvatarUrl { get; set; }
}
