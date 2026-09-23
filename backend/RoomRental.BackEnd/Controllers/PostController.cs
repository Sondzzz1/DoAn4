using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomRental.BackEnd.DTO.Common;
using RoomRental.BackEnd.DTO.Post;
using RoomRental.BackEnd.BLL.Interfaces;
using RoomRental.BackEnd.Models.Enums;
using System.Security.Claims;

namespace RoomRental.BackEnd.Controllers;

/// <summary>
/// Controller xử lý Bài đăng tìm/cho thuê phòng (Mục 4, 5, 11, 13, 14, 31)
/// </summary>
[Route("api/bai-dang")]
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
    /// Tìm kiếm và lọc danh sách tin đăng phòng trọ (Mục 4: GET /api/posts)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<PostListDto>>), 200)]
    public async Task<IActionResult> SearchPosts([FromQuery] PostQueryParameters queryParams)
    {
        try
        {
            var posts = await _postService.SearchPostsAsync(queryParams);
            return Ok(ApiResponse<List<PostListDto>>.SuccessResponse(posts, "Lấy danh sách tin đăng thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tìm kiếm bài đăng");
            return BadRequest(ApiResponse<List<PostListDto>>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Xem chi tiết phòng / tin đăng (Mục 5 & 31: GET /api/posts/{id})
    /// </summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PostDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<PostDto>), 404)]
    public async Task<IActionResult> GetPostById(int id)
    {
        try
        {
            var post = await _postService.GetPostByIdAsync(id, incrementView: true);
            return Ok(ApiResponse<PostDto>.SuccessResponse(post, "Lấy chi tiết tin đăng thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy chi tiết tin đăng ID: {Id}", id);
            return NotFound(ApiResponse<PostDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Landlord đăng tin mới (Mục 11: POST /api/posts)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Landlord,Admin")]
    [ProducesResponseType(typeof(ApiResponse<PostDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<PostDto>), 400)]
    public async Task<IActionResult> CreatePost([FromBody] CreatePostDto createDto)
    {
        try
        {
            var accountId = GetCurrentUserId();
            var post = await _postService.CreatePostAsync(accountId, createDto);

            return Ok(ApiResponse<PostDto>.SuccessResponse(post, "Đăng tin thành công. Tin của bạn đang chờ Admin duyệt."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi đăng tin mới");
            return BadRequest(ApiResponse<PostDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Landlord lấy danh sách tin đã đăng của mình (GET /api/posts/my-posts)
    /// </summary>
    [HttpGet("cua-toi")]
    [Authorize(Roles = "Landlord,Admin")]
    [ProducesResponseType(typeof(ApiResponse<List<PostListDto>>), 200)]
    public async Task<IActionResult> GetMyPosts()
    {
        try
        {
            var accountId = GetCurrentUserId();
            var posts = await _postService.GetMyPostsAsync(accountId);

            return Ok(ApiResponse<List<PostListDto>>.SuccessResponse(posts, "Lấy danh sách tin đăng của bạn thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách tin đăng của chủ trọ");
            return BadRequest(ApiResponse<List<PostListDto>>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Landlord chỉnh sửa tin đã đăng (Mục 13: PUT /api/posts/{id})
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Landlord,Admin")]
    [ProducesResponseType(typeof(ApiResponse<PostDto>), 200)]
    public async Task<IActionResult> UpdatePost(int id, [FromBody] UpdatePostDto updateDto)
    {
        try
        {
            var accountId = GetCurrentUserId();
            var post = await _postService.UpdatePostAsync(accountId, id, updateDto);

            return Ok(ApiResponse<PostDto>.SuccessResponse(post, "Cập nhật tin đăng thành công. Tin được chuyển sang trạng thái chờ duyệt lại."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật tin đăng ID: {Id}", id);
            return BadRequest(ApiResponse<PostDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Landlord xóa/ẩn tin đã đăng (Mục 14: DELETE /api/posts/{id})
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Landlord,Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> DeletePost(int id)
    {
        try
        {
            var accountId = GetCurrentUserId();
            await _postService.DeletePostAsync(accountId, id);

            return Ok(ApiResponse<object?>.SuccessResponse(null, "Đã ẩn/xóa tin đăng thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa tin đăng ID: {Id}", id);
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Landlord cập nhật trạng thái tin đăng (PUT /api/posts/{id}/status)
    /// </summary>
    [HttpPut("{id:int}/trang-thai")]
    [Authorize(Roles = "Landlord,Admin")]
    [ProducesResponseType(typeof(ApiResponse<PostDto>), 200)]
    public async Task<IActionResult> UpdatePostStatus(int id, [FromBody] UpdatePostStatusDto statusDto)
    {
        try
        {
            var accountId = GetCurrentUserId();
            var post = await _postService.UpdatePostStatusAsync(accountId, id, statusDto.Status);

            return Ok(ApiResponse<PostDto>.SuccessResponse(post, "Cập nhật trạng thái tin thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật trạng thái tin ID: {Id}", id);
            return BadRequest(ApiResponse<PostDto>.ErrorResponse(ex.Message));
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
