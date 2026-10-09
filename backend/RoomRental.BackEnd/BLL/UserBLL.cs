using Microsoft.EntityFrameworkCore;
using RoomRental.BackEnd.DTO.User;
using RoomRental.BackEnd.BLL.Interfaces;
using RoomRental.BackEnd.Models;
using RoomRental.BackEnd.DAL;

namespace RoomRental.BackEnd.BLL;

/// <summary>
/// Service xử lý User Profile
/// </summary>
public class UserBLL : IUserService
{
    private readonly ApplicationDbContext _context;

    public UserBLL(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lấy thông tin profile
    /// </summary>
    public async Task<UserDto> GetProfileAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.TenantProfile)
            .Include(u => u.LandlordProfile)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            throw BusinessRuleException.NotFound("Không tìm thấy người dùng");
        }

        return new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Phone = user.Phone ?? string.Empty,
            AvatarUrl = user.AvatarUrl,
            Address = user.RoleId == 2 ? user.LandlordProfile?.Address : user.TenantProfile?.CurrentAddress,
            Introduction = user.RoleId == 2 ? user.LandlordProfile?.Introduction : user.TenantProfile?.Introduction,
            Role = user.RoleName,
            IsBlocked = user.IsBlocked,
            CreatedAt = user.CreatedAt
        };
    }

    /// <summary>
    /// Cập nhật profile
    /// </summary>
    public async Task<UserDto> UpdateProfileAsync(int userId, UpdateProfileDto updateDto)
    {
        var user = await _context.Users
            .Include(u => u.TenantProfile)
            .Include(u => u.LandlordProfile)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            throw BusinessRuleException.NotFound("Không tìm thấy người dùng");
        }

        var fullName = updateDto.GetFullName();
        var phone = updateDto.GetPhone();
        var avatarUrl = updateDto.GetAvatarUrl();
        var address = updateDto.GetAddress();
        var introduction = updateDto.GetIntroduction();

        if (!string.IsNullOrWhiteSpace(fullName)) user.FullName = fullName;
        if (!string.IsNullOrWhiteSpace(phone)) user.Phone = phone;
        if (avatarUrl != null) user.AvatarUrl = avatarUrl;
        user.UpdatedAt = DateTime.Now;

        if (user.RoleId == 1)
        {
            user.TenantProfile ??= new TenantProfile { AccountId = user.Id };
            if (address != null) user.TenantProfile.CurrentAddress = address;
            if (introduction != null) user.TenantProfile.Introduction = introduction;
        }
        else if (user.RoleId == 2)
        {
            user.LandlordProfile ??= new LandlordProfile { AccountId = user.Id };
            if (address != null) user.LandlordProfile.Address = address;
            if (introduction != null) user.LandlordProfile.Introduction = introduction;
        }

        await _context.SaveChangesAsync();

        return new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Phone = user.Phone ?? string.Empty,
            AvatarUrl = user.AvatarUrl,
            Address = user.RoleId == 2 ? user.LandlordProfile?.Address : user.TenantProfile?.CurrentAddress,
            Introduction = user.RoleId == 2 ? user.LandlordProfile?.Introduction : user.TenantProfile?.Introduction,
            Role = user.RoleName,
            IsBlocked = user.IsBlocked,
            CreatedAt = user.CreatedAt
        };
    }

    /// <summary>
    /// Đổi mật khẩu
    /// </summary>
    public async Task ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto)
    {
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
        {
            throw BusinessRuleException.NotFound("Không tìm thấy người dùng");
        }

        var currentPassword = changePasswordDto.GetCurrentPassword();
        var newPassword = changePasswordDto.GetNewPassword();

        if (string.IsNullOrWhiteSpace(currentPassword))
        {
            throw new BusinessRuleException("Vui lòng nhập mật khẩu hiện tại");
        }

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            throw new BusinessRuleException("Mật khẩu mới phải có ít nhất 6 ký tự");
        }

        // Verify mật khẩu hiện tại
        var isCurrentPasswordValid = BCrypt.Net.BCrypt.Verify(
            currentPassword, 
            user.PasswordHash
        );

        if (!isCurrentPasswordValid)
        {
            throw new BusinessRuleException("Mật khẩu hiện tại không đúng");
        }

        // Hash mật khẩu mới
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();
    }
}
