using RoomRental.BackEnd.DTO.Admin;
using RoomRental.BackEnd.DTO.Dashboard;
using RoomRental.BackEnd.DTO.Post;
using RoomRental.BackEnd.DTO.Room;
using RoomRental.BackEnd.Models.Enums;

namespace RoomRental.BackEnd.BLL.Interfaces;

public interface IAdminService
{
    // Dashboard (Mục 20)
    Task<AdminDashboardDto> GetDashboardAsync();

    // Landlord Dashboard (Mục 18)
    Task<LandlordDashboardDto> GetLandlordDashboardAsync(int landlordAccountId);

    // User Management (Mục 21, 22)
    Task<List<AdminUserDto>> GetAllUsersAsync(string? keyword = null, int? roleId = null);
    Task<List<AdminLandlordDto>> GetAllLandlordsAsync(string? keyword = null);
    Task<AdminUserDto> LockUserAsync(int userId);
    Task<AdminUserDto> UnlockUserAsync(int userId);

    // Post Management & Moderation (Mục 23, 24, 25)
    Task<List<PostListDto>> GetAllPostsAsync(PostStatus? status = null, string? keyword = null);
    Task<PostDto> ApprovePostAsync(int postId);
    Task<PostDto> RejectPostAsync(int postId, string reason);
    Task<PostDto> HidePostAsync(int postId);

    // Room Management (Mục 26)
    Task<List<RoomDto>> GetAllRoomsAsync(string? keyword = null, RoomStatus? status = null);
}
