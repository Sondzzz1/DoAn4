using System.ComponentModel.DataAnnotations.Schema;

namespace RoomRental.BackEnd.Models;

/// <summary>
/// Entity TaiKhoan - tài khoản đăng nhập trong database RoomRentalDB.
/// VaiTro: 0 = Admin, 1 = Tenant/NguoiDung, 2 = Landlord/ChuTro.
/// </summary>
public class User
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
    public int RoleId { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    [NotMapped]
    public bool IsBlocked
    {
        get => !IsActive;
        set => IsActive = !value;
    }

    [NotMapped]
    public string RoleName => RoleId switch
    {
        0 => "Admin",
        2 => "Landlord",
        _ => "Tenant"
    };

    public virtual TenantProfile? TenantProfile { get; set; }
    public virtual LandlordProfile? LandlordProfile { get; set; }
}
