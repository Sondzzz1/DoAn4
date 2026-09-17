using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomRental.Application.DTOs.Common;
using RoomRental.Application.DTOs.Dashboard;
using RoomRental.Application.Interfaces;
using System.Security.Claims;

namespace RoomRental.API.Controllers;

/// <summary>
/// Controller xử lý Dashboard & Thống kê dành cho Chủ trọ (Mục 18)
/// </summary>
[Route("api/chu-tro")]
[ApiController]
[Authorize(Roles = "Landlord,Admin")]
public class LandlordController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly ILogger<LandlordController> _logger;

    public LandlordController(IAdminService adminService, ILogger<LandlordController> logger)
    {
        _adminService = adminService;
        _logger = logger;
    }

    /// <summary>
    /// Dashboard thống kê cho Chủ trọ (Mục 18: GET /api/landlords/dashboard)
    /// </summary>
    [HttpGet("tong-quan")]
    [ProducesResponseType(typeof(ApiResponse<LandlordDashboardDto>), 200)]
    public async Task<IActionResult> GetDashboard()
    {
        try
        {
            var accountId = GetCurrentUserId();
            var dashboard = await _adminService.GetLandlordDashboardAsync(accountId);

            return Ok(ApiResponse<LandlordDashboardDto>.SuccessResponse(dashboard, "Lấy dữ liệu Dashboard Chủ trọ thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy dữ liệu Dashboard Chủ trọ");
            return BadRequest(ApiResponse<LandlordDashboardDto>.ErrorResponse(ex.Message));
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
