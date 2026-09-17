using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomRental.Application.DTOs.Common;
using RoomRental.Application.DTOs.Report;
using RoomRental.Application.Interfaces;
using System.Security.Claims;

namespace RoomRental.API.Controllers;

/// <summary>
/// Controller xử lý Báo cáo vi phạm (Mục 27)
/// </summary>
[Route("api/bao-cao")]
[ApiController]
public class ReportController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly ILogger<ReportController> _logger;

    public ReportController(IReportService reportService, ILogger<ReportController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    /// <summary>
    /// Gửi báo cáo vi phạm tin đăng (Mục 27: POST /api/reports)
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ReportDto>), 200)]
    public async Task<IActionResult> CreateReport([FromBody] CreateReportDto createDto)
    {
        try
        {
            var accountId = GetCurrentUserId();
            var report = await _reportService.CreateReportAsync(accountId, createDto);

            return Ok(ApiResponse<ReportDto>.SuccessResponse(report, "Gửi báo cáo vi phạm thành công. Đội ngũ kiểm duyệt sẽ xử lý trong thời gian sớm nhất."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gửi báo cáo vi phạm");
            return BadRequest(ApiResponse<ReportDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Admin lấy danh sách báo cáo vi phạm (Mục 27: GET /api/reports)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<List<ReportDto>>), 200)]
    public async Task<IActionResult> GetReports([FromQuery] int? status)
    {
        try
        {
            var reports = await _reportService.GetReportsAsync(status);
            return Ok(ApiResponse<List<ReportDto>>.SuccessResponse(reports, "Lấy danh sách báo cáo thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách báo cáo");
            return BadRequest(ApiResponse<List<ReportDto>>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Xem chi tiết 1 báo cáo
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<ReportDto>), 200)]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var report = await _reportService.GetReportByIdAsync(id);
            return Ok(ApiResponse<ReportDto>.SuccessResponse(report, "Lấy chi tiết báo cáo thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy chi tiết báo cáo ID: {Id}", id);
            return BadRequest(ApiResponse<ReportDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Admin cập nhật trạng thái báo cáo (Mục 27: PUT /api/reports/{id}/status)
    /// </summary>
    [HttpPut("{id:int}/trang-thai")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<ReportDto>), 200)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateReportStatusDto updateDto)
    {
        try
        {
            var accountId = GetCurrentUserId();
            var report = await _reportService.UpdateReportStatusAsync(accountId, id, updateDto);

            return Ok(ApiResponse<ReportDto>.SuccessResponse(report, "Cập nhật trạng thái báo cáo thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật trạng thái báo cáo ID: {Id}", id);
            return BadRequest(ApiResponse<ReportDto>.ErrorResponse(ex.Message));
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
