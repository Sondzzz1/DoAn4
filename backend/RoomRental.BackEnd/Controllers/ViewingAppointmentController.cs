using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomRental.BackEnd.DTO.Common;
using RoomRental.BackEnd.DTO.ViewingAppointment;
using RoomRental.BackEnd.BLL.Interfaces;
using RoomRental.BackEnd.Models.Enums;
using System.Security.Claims;

namespace RoomRental.BackEnd.Controllers;

/// <summary>
/// Controller xử lý Đặt lịch xem phòng (Mục 7, 8, 17)
/// </summary>
[Route("api/lich-hen-xem-phong")]
[ApiController]
[Authorize]
public class ViewingAppointmentController : ControllerBase
{
    private readonly IViewingAppointmentService _appointmentService;
    private readonly ILogger<ViewingAppointmentController> _logger;

    public ViewingAppointmentController(IViewingAppointmentService appointmentService, ILogger<ViewingAppointmentController> logger)
    {
        _appointmentService = appointmentService;
        _logger = logger;
    }

    /// <summary>
    /// Tenant đặt lịch xem phòng (Mục 7: POST /api/viewing-appointments)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), 200)]
    public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentDto createDto)
    {
        try
        {
            var accountId = GetCurrentUserId();
            var appointment = await _appointmentService.CreateAppointmentAsync(accountId, createDto);

            return Ok(ApiResponse<AppointmentDto>.SuccessResponse(appointment, "Đặt lịch xem phòng thành công. Đang chờ chủ trọ xác nhận."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi đặt lịch xem phòng");
            return BadRequest(ApiResponse<AppointmentDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Tenant xem danh sách lịch hẹn của mình (GET /api/viewing-appointments/my)
    /// </summary>
    [HttpGet("cua-toi")]
    [ProducesResponseType(typeof(ApiResponse<List<AppointmentDto>>), 200)]
    public async Task<IActionResult> GetMyAppointments()
    {
        try
        {
            var accountId = GetCurrentUserId();
            var appointments = await _appointmentService.GetTenantAppointmentsAsync(accountId);

            return Ok(ApiResponse<List<AppointmentDto>>.SuccessResponse(appointments, "Lấy danh sách lịch hẹn thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách lịch hẹn của Tenant");
            return BadRequest(ApiResponse<List<AppointmentDto>>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Landlord xem danh sách lịch hẹn (Mục 17: GET /api/viewing-appointments/landlord)
    /// </summary>
    [HttpGet("chu-tro")]
    [Authorize(Roles = "Landlord,Admin")]
    [ProducesResponseType(typeof(ApiResponse<List<AppointmentDto>>), 200)]
    public async Task<IActionResult> GetLandlordAppointments([FromQuery] AppointmentStatus? status)
    {
        try
        {
            var accountId = GetCurrentUserId();
            var appointments = await _appointmentService.GetLandlordAppointmentsAsync(accountId, status);

            return Ok(ApiResponse<List<AppointmentDto>>.SuccessResponse(appointments, "Lấy danh sách lịch hẹn của chủ trọ thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách lịch hẹn của Landlord");
            return BadRequest(ApiResponse<List<AppointmentDto>>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Tenant hủy lịch xem phòng (Mục 8: PUT /api/viewing-appointments/{id}/cancel)
    /// </summary>
    [HttpPut("{id:int}/huy")]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), 200)]
    public async Task<IActionResult> CancelAppointment(int id, [FromBody] UpdateAppointmentStatusDto? dto)
    {
        try
        {
            var accountId = GetCurrentUserId();
            var appointment = await _appointmentService.CancelAppointmentAsync(accountId, id, dto?.GetReason());

            return Ok(ApiResponse<AppointmentDto>.SuccessResponse(appointment, "Hủy lịch hẹn thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi hủy lịch hẹn ID: {Id}", id);
            return BadRequest(ApiResponse<AppointmentDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Landlord xác nhận lịch hẹn (Mục 17: PUT /api/viewing-appointments/{id}/confirm)
    /// </summary>
    [HttpPut("{id:int}/xac-nhan")]
    [Authorize(Roles = "Landlord,Admin")]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), 200)]
    public async Task<IActionResult> ConfirmAppointment(int id)
    {
        try
        {
            var accountId = GetCurrentUserId();
            var appointment = await _appointmentService.ConfirmAppointmentAsync(accountId, id);

            return Ok(ApiResponse<AppointmentDto>.SuccessResponse(appointment, "Đã xác nhận lịch hẹn"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xác nhận lịch hẹn ID: {Id}", id);
            return BadRequest(ApiResponse<AppointmentDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Landlord từ chối lịch hẹn (Mục 17: PUT /api/viewing-appointments/{id}/reject)
    /// </summary>
    [HttpPut("{id:int}/tu-choi")]
    [Authorize(Roles = "Landlord,Admin")]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), 200)]
    public async Task<IActionResult> RejectAppointment(int id, [FromBody] UpdateAppointmentStatusDto? dto)
    {
        try
        {
            var accountId = GetCurrentUserId();
            var appointment = await _appointmentService.RejectAppointmentAsync(accountId, id, dto?.GetReason());

            return Ok(ApiResponse<AppointmentDto>.SuccessResponse(appointment, "Đã từ chối lịch hẹn"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi từ chối lịch hẹn ID: {Id}", id);
            return BadRequest(ApiResponse<AppointmentDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Landlord hoàn thành lịch hẹn (Mục 17: PUT /api/viewing-appointments/{id}/complete)
    /// </summary>
    [HttpPut("{id:int}/hoan-thanh")]
    [Authorize(Roles = "Landlord,Admin")]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), 200)]
    public async Task<IActionResult> CompleteAppointment(int id)
    {
        try
        {
            var accountId = GetCurrentUserId();
            var appointment = await _appointmentService.CompleteAppointmentAsync(accountId, id);

            return Ok(ApiResponse<AppointmentDto>.SuccessResponse(appointment, "Đã hoàn thành buổi xem phòng"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi hoàn tất lịch hẹn ID: {Id}", id);
            return BadRequest(ApiResponse<AppointmentDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Xem chi tiết 1 lịch hẹn (GET /api/viewing-appointments/{id})
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), 200)]
    public async Task<IActionResult> GetAppointmentById(int id)
    {
        try
        {
            var accountId = GetCurrentUserId();
            var appointment = await _appointmentService.GetAppointmentByIdAsync(accountId, id);

            return Ok(ApiResponse<AppointmentDto>.SuccessResponse(appointment, "Lấy chi tiết lịch hẹn thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy chi tiết lịch hẹn ID: {Id}", id);
            return BadRequest(ApiResponse<AppointmentDto>.ErrorResponse(ex.Message));
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
