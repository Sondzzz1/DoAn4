using RoomRental.Application.DTOs.Post;
using RoomRental.Domain.Enums;

namespace RoomRental.Application.Interfaces;

/// <summary>
/// Interface cho Post Service
/// </summary>
public interface IPostService
{
    /// <summary>
    /// Tạo post mới (Landlord)
    /// </summary>
    Task<PostDto> CreatePostAsync(int landlordId, CreatePostDto createDto);

    /// <summary>
    /// Lấy danh sách post của landlord
    /// </summary>
    Task<List<PostListDto>> GetMyPostsAsync(int landlordId);

    /// <summary>
    /// Lấy chi tiết 1 post
    /// </summary>
    Task<PostDto> GetPostByIdAsync(int postId);

    /// <summary>
    /// Cập nhật post (chỉ landlord sở hữu)
    /// </summary>
    Task<PostDto> UpdatePostAsync(int landlordId, int postId, UpdatePostDto updateDto);

    /// <summary>
    /// Xóa post (chỉ landlord sở hữu)
    /// </summary>
    Task DeletePostAsync(int landlordId, int postId);

    /// <summary>
    /// Cập nhật trạng thái post (Landlord: Hidden/Pending only)
    /// </summary>
    Task<PostDto> UpdatePostStatusAsync(int landlordId, int postId, PostStatus status);

    /// <summary>
    /// Lấy danh sách post công khai (cho Tenant search)
    /// </summary>
    Task<List<PostListDto>> GetPublicPostsAsync(string? province = null, string? district = null);
}
