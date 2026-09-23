using RoomRental.BackEnd.DTO.Post;
using RoomRental.BackEnd.Models.Enums;

namespace RoomRental.BackEnd.BLL.Interfaces;

/// <summary>
/// Interface cho Post Service
/// </summary>
public interface IPostService
{
    /// <summary>
    /// Tìm kiếm và lọc bài đăng công khai (cho Tenant & Khách vãng lai)
    /// </summary>
    Task<List<PostListDto>> SearchPostsAsync(PostQueryParameters queryParams);

    /// <summary>
    /// Lấy chi tiết 1 post kèm đầy đủ thông tin phòng, tiện ích, ảnh, tọa độ maps
    /// </summary>
    Task<PostDto> GetPostByIdAsync(int postId, bool incrementView = true);

    /// <summary>
    /// Tạo post mới (Landlord đăng tin)
    /// </summary>
    Task<PostDto> CreatePostAsync(int accountId, CreatePostDto createDto);

    /// <summary>
    /// Lấy danh sách post của landlord
    /// </summary>
    Task<List<PostListDto>> GetMyPostsAsync(int accountId);

    /// <summary>
    /// Cập nhật post (chỉ landlord sở hữu)
    /// </summary>
    Task<PostDto> UpdatePostAsync(int accountId, int postId, UpdatePostDto updateDto);

    /// <summary>
    /// Xóa mềm post (chỉ landlord sở hữu)
    /// </summary>
    Task DeletePostAsync(int accountId, int postId);

    /// <summary>
    /// Cập nhật trạng thái post bởi Landlord
    /// </summary>
    Task<PostDto> UpdatePostStatusAsync(int accountId, int postId, PostStatus status);
}
