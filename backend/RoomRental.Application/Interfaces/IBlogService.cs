using RoomRental.Application.DTOs.Blog;

namespace RoomRental.Application.Interfaces;

public interface IBlogService
{
    Task<List<BlogPostDto>> GetPublishedBlogPostsAsync();
    Task<List<BlogPostDto>> GetAllBlogPostsAsync();
    Task<BlogPostDto> GetBlogPostByIdAsync(int id, bool incrementView = true);
    Task<BlogPostDto> GetBlogPostBySlugAsync(string slug, bool incrementView = true);
    Task<BlogPostDto> CreateBlogPostAsync(int authorAccountId, CreateBlogPostDto createDto);
    Task<BlogPostDto> UpdateBlogPostAsync(int id, UpdateBlogPostDto updateDto);
    Task DeleteBlogPostAsync(int id);

    // Comments
    Task<BlogCommentDto> AddCommentAsync(int blogPostId, int accountId, CreateBlogCommentDto createDto);
    Task DeleteCommentAsync(int commentId, int accountId, bool isAdmin = false);
}
