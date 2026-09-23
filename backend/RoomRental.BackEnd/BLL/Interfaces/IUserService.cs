using RoomRental.BackEnd.DTO.User;

namespace RoomRental.BackEnd.BLL.Interfaces;

/// <summary>
/// Interface cho User Service
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Lấy thông tin profile của user hiện tại
    /// </summary>
    Task<UserDto> GetProfileAsync(int userId);

    /// <summary>
    /// Cập nhật thông tin profile
    /// </summary>
    Task<UserDto> UpdateProfileAsync(int userId, UpdateProfileDto updateDto);

    /// <summary>
    /// Đổi mật khẩu
    /// </summary>
    Task ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto);
}
