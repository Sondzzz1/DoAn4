using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomRental.Application.DTOs.Admin;
using RoomRental.Application.DTOs.Common;
using RoomRental.Application.DTOs.Dashboard;
using RoomRental.Application.DTOs.Post;
using RoomRental.Application.DTOs.Room;
using RoomRental.Application.Interfaces;
using RoomRental.Domain.Enums;

namespace RoomRental.API.Controllers;

/// <summary>
/// Controller xử lý Toàn bộ chức năng Quản trị của Admin (Mục 20, 21, 22, 23, 24, 25, 26)
/// </summary>
[Route("api/quan-tri")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IAdminService adminService, ILogger<AdminController> logger)
    {
        _adminService = adminService;
        _logger = logger;
    }

    /// <summary>
    /// Admin Dashboard thống kê tổng quan (Mục 20: GET /api/admin/dashboard)
    /// </summary>
    [HttpGet("tong-quan")]
    [ProducesResponseType(typeof(ApiResponse<AdminDashboardDto>), 200)]
    public async Task<IActionResult> GetDashboard()
    {
        try
        {
            var dashboard = await _adminService.GetDashboardAsync();
            return Ok(ApiResponse<AdminDashboardDto>.SuccessResponse(dashboard, "Lấy dữ liệu Dashboard Admin thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy dữ liệu Dashboard Admin");
            return BadRequest(ApiResponse<AdminDashboardDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Admin quản lý danh sách tài khoản (Mục 21: GET /api/admin/users)
    /// </summary>
    [HttpGet("nguoi-dung")]
    [ProducesResponseType(typeof(ApiResponse<List<AdminUserDto>>), 200)]
    public async Task<IActionResult> GetUsers([FromQuery] string? keyword, [FromQuery] int? roleId)
    {
        try
        {
            var users = await _adminService.GetAllUsersAsync(keyword, roleId);
            return Ok(ApiResponse<List<AdminUserDto>>.SuccessResponse(users, "Lấy danh sách người dùng thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách người dùng");
            return BadRequest(ApiResponse<List<AdminUserDto>>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Khóa tài khoản người dùng (Mục 21: PUT /api/admin/users/{id}/lock)
    /// </summary>
    [HttpPut("nguoi-dung/{id:int}/khoa")]
    [ProducesResponseType(typeof(ApiResponse<AdminUserDto>), 200)]
    public async Task<IActionResult> LockUser(int id)
    {
        try
        {
            var user = await _adminService.LockUserAsync(id);
            return Ok(ApiResponse<AdminUserDto>.SuccessResponse(user, "Đã khóa tài khoản thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi khóa tài khoản ID: {Id}", id);
            return BadRequest(ApiResponse<AdminUserDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Mở khóa tài khoản người dùng (Mục 21: PUT /api/admin/users/{id}/unlock)
    /// </summary>
    [HttpPut("nguoi-dung/{id:int}/mo-khoa")]
    [ProducesResponseType(typeof(ApiResponse<AdminUserDto>), 200)]
    public async Task<IActionResult> UnlockUser(int id)
    {
        try
        {
            var user = await _adminService.UnlockUserAsync(id);
            return Ok(ApiResponse<AdminUserDto>.SuccessResponse(user, "Đã mở khóa tài khoản thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi mở khóa tài khoản ID: {Id}", id);
            return BadRequest(ApiResponse<AdminUserDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Admin xem danh sách chủ trọ (Mục 22: GET /api/admin/landlords)
    /// </summary>
    [HttpGet("chu-tro")]
    [ProducesResponseType(typeof(ApiResponse<List<AdminLandlordDto>>), 200)]
    public async Task<IActionResult> GetLandlords([FromQuery] string? keyword)
    {
        try
        {
            var landlords = await _adminService.GetAllLandlordsAsync(keyword);
            return Ok(ApiResponse<List<AdminLandlordDto>>.SuccessResponse(landlords, "Lấy danh sách chủ trọ thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách chủ trọ");
            return BadRequest(ApiResponse<List<AdminLandlordDto>>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Admin xem và lọc danh sách bài đăng (Mục 23: GET /api/admin/posts)
    /// </summary>
    [HttpGet("bai-dang")]
    [ProducesResponseType(typeof(ApiResponse<List<PostListDto>>), 200)]
    public async Task<IActionResult> GetPosts([FromQuery] PostStatus? status, [FromQuery] string? keyword)
    {
        try
        {
            var posts = await _adminService.GetAllPostsAsync(status, keyword);
            return Ok(ApiResponse<List<PostListDto>>.SuccessResponse(posts, "Lấy danh sách bài đăng thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách bài đăng cho Admin");
            return BadRequest(ApiResponse<List<PostListDto>>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Duyệt bài đăng (Mục 24: PUT /api/admin/posts/{id}/approve)
    /// </summary>
    [HttpPut("bai-dang/{id:int}/duyet")]
    [ProducesResponseType(typeof(ApiResponse<PostDto>), 200)]
    public async Task<IActionResult> ApprovePost(int id)
    {
        try
        {
            var post = await _adminService.ApprovePostAsync(id);
            return Ok(ApiResponse<PostDto>.SuccessResponse(post, "Đã duyệt bài đăng thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi duyệt bài đăng ID: {Id}", id);
            return BadRequest(ApiResponse<PostDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Từ chối bài đăng kèm lý do (Mục 25: PUT /api/admin/posts/{id}/reject)
    /// </summary>
    [HttpPut("bai-dang/{id:int}/tu-choi")]
    [ProducesResponseType(typeof(ApiResponse<PostDto>), 200)]
    public async Task<IActionResult> RejectPost(int id, [FromBody] RejectPostDto dto)
    {
        try
        {
            var post = await _adminService.RejectPostAsync(id, dto.GetReason());
            return Ok(ApiResponse<PostDto>.SuccessResponse(post, "Đã từ chối bài đăng"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi từ chối bài đăng ID: {Id}", id);
            return BadRequest(ApiResponse<PostDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Ẩn bài đăng vi phạm (PUT /api/admin/posts/{id}/hide)
    /// </summary>
    [HttpPut("bai-dang/{id:int}/an")]
    [ProducesResponseType(typeof(ApiResponse<PostDto>), 200)]
    public async Task<IActionResult> HidePost(int id)
    {
        try
        {
            var post = await _adminService.HidePostAsync(id);
            return Ok(ApiResponse<PostDto>.SuccessResponse(post, "Đã ẩn bài đăng"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi ẩn bài đăng ID: {Id}", id);
            return BadRequest(ApiResponse<PostDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Admin xem toàn bộ phòng trong hệ thống (Mục 26: GET /api/admin/rooms)
    /// </summary>
    [HttpGet("phong")]
    [ProducesResponseType(typeof(ApiResponse<List<RoomDto>>), 200)]
    public async Task<IActionResult> GetRooms([FromQuery] string? keyword, [FromQuery] RoomStatus? status)
    {
        try
        {
            var rooms = await _adminService.GetAllRoomsAsync(keyword, status);
            return Ok(ApiResponse<List<RoomDto>>.SuccessResponse(rooms, "Lấy danh sách phòng thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách phòng cho Admin");
            return BadRequest(ApiResponse<List<RoomDto>>.ErrorResponse(ex.Message));
        }
    }
}
