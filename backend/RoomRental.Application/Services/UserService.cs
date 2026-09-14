using Microsoft.EntityFrameworkCore;
using RoomRental.Application.DTOs.User;
using RoomRental.Application.Interfaces;
using RoomRental.Infrastructure.Data;

namespace RoomRental.Application.Services;

/// <summary>
/// Service xử lý User Profile
/// </summary>
public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;

    public UserService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lấy thông tin profile
    /// </summary>
    public async Task<UserDto> GetProfileAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            throw new Exception("Không tìm thấy người dùng");
        }

        return new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            AvatarUrl = user.AvatarUrl,
            Role = user.Role.Name,
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
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            throw new Exception("Không tìm thấy người dùng");
        }

        // Cập nhật thông tin
        user.FullName = updateDto.FullName;
        user.Phone = updateDto.Phone;
        user.AvatarUrl = updateDto.AvatarUrl;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            AvatarUrl = user.AvatarUrl,
            Role = user.Role.Name,
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
            throw new Exception("Không tìm thấy người dùng");
        }

        // Verify mật khẩu hiện tại
        var isCurrentPasswordValid = BCrypt.Net.BCrypt.Verify(
            changePasswordDto.CurrentPassword, 
            user.PasswordHash
        );

        if (!isCurrentPasswordValid)
        {
            throw new Exception("Mật khẩu hiện tại không đúng");
        }

        // Hash mật khẩu mới
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(changePasswordDto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }
}
