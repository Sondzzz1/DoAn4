using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomRental.Application.DTOs.Common;
using RoomRental.Application.DTOs.Post;
using RoomRental.Application.Interfaces;
using RoomRental.Domain.Enums;
using System.Security.Claims;

namespace RoomRental.API.Controllers;

/// <summary>
/// Controller xử lý Post Management
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class PostController : ControllerBase
{
    private readonly IPostService _postService;
    private readonly ILogger<PostController> _logger;

    public PostController(IPostService postService, ILogger<PostController> logger)
    {
        _postService = postService;
        _logger = logger;
    }

    /// <summary>
    /// Tạo post mới (Landlord only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Landlord")]
    [ProducesResponseType(typeof(ApiResponse<PostDto>), 200)]
    public async Task<IActionResult> CreatePost([FromBody] CreatePostDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<PostDto>.ErrorResponse("Dữ liệu không hợp lệ", errors));
            }

            var userId = GetCurrentUserId();
            var post = await _postService.CreatePostAsync(userId, createDto);

            return Ok(ApiResponse<PostDto>.SuccessResponse(post, "Tạo tin đăng thành công. Tin của bạn đang chờ Admin duyệt"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo post");
            return BadRequest(ApiResponse<PostDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Lấy danh sách post của landlord (My Posts)
    /// </summary>
    [HttpGet("my-posts")]
    [Authorize(Roles = "Landlord")]
    [ProducesResponseType(typeof(ApiResponse<List<PostListDto>>), 200)]
    public async Task<IActionResult> GetMyPosts()
    {
        try
        {
            var userId = GetCurrentUserId();
            var posts = await _postService.GetMyPostsAsync(userId);

            return Ok(ApiResponse<List<PostListDto>>.SuccessResponse(posts, $"Lấy danh sách thành công. Tổng: {posts.Count} tin"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách post");
            return BadRequest(ApiResponse<List<PostListDto>>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Lấy chi tiết post theo ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PostDto>), 200)]
    public async Task<IActionResult> GetPostById(int id)
    {
        try
        {
            var post = await _postService.GetPostByIdAsync(id);
            return Ok(ApiResponse<PostDto>.SuccessResponse(post, "Lấy thông tin thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy chi tiết post");
            return BadRequest(ApiResponse<PostDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Cập nhật post (Landlord only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Landlord")]
    [ProducesResponseType(typeof(ApiResponse<PostDto>), 200)]
    public async Task<IActionResult> UpdatePost(int id, [FromBody] UpdatePostDto updateDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<PostDto>.ErrorResponse("Dữ liệu không hợp lệ", errors));
            }

            var userId = GetCurrentUserId();
            var post = await _postService.UpdatePostAsync(userId, id, updateDto);

            return Ok(ApiResponse<PostDto>.SuccessResponse(post, "Cập nhật tin đăng thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật post");
            return BadRequest(ApiResponse<PostDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Xóa post (Landlord only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Landlord")]
    [ProducesResponseType(typeof(ApiResponse<object?>), 200)]
    public async Task<IActionResult> DeletePost(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _postService.DeletePostAsync(userId, id);

            return Ok(ApiResponse<object?>.SuccessResponse(null, "Xóa tin đăng thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa post");
            return BadRequest(ApiResponse<object?>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Cập nhật trạng thái post (Landlord: Hidden/Pending only)
    /// </summary>
    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Landlord")]
    [ProducesResponseType(typeof(ApiResponse<PostDto>), 200)]
    public async Task<IActionResult> UpdatePostStatus(int id, [FromBody] UpdatePostStatusDto statusDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var post = await _postService.UpdatePostStatusAsync(userId, id, statusDto.Status);

            return Ok(ApiResponse<PostDto>.SuccessResponse(post, "Cập nhật trạng thái thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật trạng thái post");
            return BadRequest(ApiResponse<PostDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Lấy danh sách post công khai (Public - cho Tenant search)
    /// </summary>
    [HttpGet("public")]
    [ProducesResponseType(typeof(ApiResponse<List<PostListDto>>), 200)]
    public async Task<IActionResult> GetPublicPosts([FromQuery] string? province = null, [FromQuery] string? district = null)
    {
        try
        {
            var posts = await _postService.GetPublicPostsAsync(province, district);
            return Ok(ApiResponse<List<PostListDto>>.SuccessResponse(posts, $"Lấy danh sách thành công. Tổng: {posts.Count} tin"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách post công khai");
            return BadRequest(ApiResponse<List<PostListDto>>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Helper: Lấy UserId từ JWT Claims
    /// </summary>
    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("UserId")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            throw new Exception("Không thể xác định user");
        }

        return userId;
    }
}
