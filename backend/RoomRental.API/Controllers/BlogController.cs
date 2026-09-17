using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomRental.Application.DTOs.Blog;
using RoomRental.Application.DTOs.Common;
using RoomRental.Application.Interfaces;
using System.Security.Claims;

namespace RoomRental.API.Controllers;

/// <summary>
/// Controller xử lý Tin tức / Blog / Kinh nghiệm thuê trọ (Mục 30)
/// </summary>
[Route("api/bai-viet")]
[ApiController]
public class BlogController : ControllerBase
{
    private readonly IBlogService _blogService;
    private readonly ILogger<BlogController> _logger;

    public BlogController(IBlogService blogService, ILogger<BlogController> logger)
    {
        _blogService = blogService;
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách bài viết blog đã xuất bản (Mục 30: GET /api/blogs)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<BlogPostDto>>), 200)]
    public async Task<IActionResult> GetPublished()
    {
        try
        {
            var blogs = await _blogService.GetPublishedBlogPostsAsync();
            return Ok(ApiResponse<List<BlogPostDto>>.SuccessResponse(blogs, "Lấy danh sách bài viết thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách blog");
            return BadRequest(ApiResponse<List<BlogPostDto>>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Lấy tất cả bài viết blog (dành cho Admin)
    /// </summary>
    [HttpGet("tat-ca")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<List<BlogPostDto>>), 200)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var blogs = await _blogService.GetAllBlogPostsAsync();
            return Ok(ApiResponse<List<BlogPostDto>>.SuccessResponse(blogs, "Lấy toàn bộ bài viết thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách blog cho admin");
            return BadRequest(ApiResponse<List<BlogPostDto>>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Xem chi tiết bài viết blog theo ID (GET /api/blogs/{id})
    /// </summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<BlogPostDto>), 200)]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var blog = await _blogService.GetBlogPostByIdAsync(id, incrementView: true);
            return Ok(ApiResponse<BlogPostDto>.SuccessResponse(blog, "Lấy chi tiết bài viết thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy bài viết ID: {Id}", id);
            return BadRequest(ApiResponse<BlogPostDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Xem chi tiết bài viết blog theo Slug (GET /api/blogs/slug/{slug})
    /// </summary>
    [HttpGet("duong-dan/{slug}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<BlogPostDto>), 200)]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        try
        {
            var blog = await _blogService.GetBlogPostBySlugAsync(slug, incrementView: true);
            return Ok(ApiResponse<BlogPostDto>.SuccessResponse(blog, "Lấy chi tiết bài viết thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy bài viết slug: {Slug}", slug);
            return BadRequest(ApiResponse<BlogPostDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Admin tạo bài viết mới (Mục 30: POST /api/blogs)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<BlogPostDto>), 200)]
    public async Task<IActionResult> Create([FromBody] CreateBlogPostDto createDto)
    {
        try
        {
            var accountId = GetCurrentUserId();
            var blog = await _blogService.CreateBlogPostAsync(accountId, createDto);

            return Ok(ApiResponse<BlogPostDto>.SuccessResponse(blog, "Tạo bài viết mới thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo bài viết mới");
            return BadRequest(ApiResponse<BlogPostDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Admin chỉnh sửa bài viết (PUT /api/blogs/{id})
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<BlogPostDto>), 200)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateBlogPostDto updateDto)
    {
        try
        {
            var blog = await _blogService.UpdateBlogPostAsync(id, updateDto);
            return Ok(ApiResponse<BlogPostDto>.SuccessResponse(blog, "Cập nhật bài viết thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật bài viết ID: {Id}", id);
            return BadRequest(ApiResponse<BlogPostDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Admin xóa bài viết (DELETE /api/blogs/{id})
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _blogService.DeleteBlogPostAsync(id);
            return Ok(ApiResponse<object?>.SuccessResponse(null, "Xóa bài viết thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa bài viết ID: {Id}", id);
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Bình luận vào bài viết (POST /api/blogs/{id}/comments)
    /// </summary>
    [HttpPost("{id:int}/binh-luan")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<BlogCommentDto>), 200)]
    public async Task<IActionResult> AddComment(int id, [FromBody] CreateBlogCommentDto createDto)
    {
        try
        {
            var accountId = GetCurrentUserId();
            var comment = await _blogService.AddCommentAsync(id, accountId, createDto);

            return Ok(ApiResponse<BlogCommentDto>.SuccessResponse(comment, "Bình luận thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi thêm bình luận");
            return BadRequest(ApiResponse<BlogCommentDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Xóa bình luận (DELETE /api/blogs/comments/{id})
    /// </summary>
    [HttpDelete("binh-luan/{id:int}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> DeleteComment(int id)
    {
        try
        {
            var accountId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin");
            await _blogService.DeleteCommentAsync(id, accountId, isAdmin);

            return Ok(ApiResponse<object?>.SuccessResponse(null, "Xóa bình luận thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa bình luận ID: {Id}", id);
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("UserId")?.Value 
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            throw new Exception("Không thể xác thực người dùng");
        }

        return userId;
    }
}
