using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomRental.BackEnd.DTO.Amenity;
using RoomRental.BackEnd.DTO.Common;
using RoomRental.BackEnd.BLL.Interfaces;

namespace RoomRental.BackEnd.Controllers;

/// <summary>
/// Controller xử lý Tiện ích phòng trọ (Mục 29)
/// </summary>
[Route("api/tien-ich")]
[ApiController]
public class AmenityController : ControllerBase
{
    private readonly IAmenityService _amenityService;
    private readonly ILogger<AmenityController> _logger;

    public AmenityController(IAmenityService amenityService, ILogger<AmenityController> logger)
    {
        _amenityService = amenityService;
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách tiện ích (Mục 29: GET /api/amenities)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<AmenityDto>>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = true)
    {
        try
        {
            var amenities = await _amenityService.GetAllAmenitiesAsync(activeOnly);
            return Ok(ApiResponse<List<AmenityDto>>.SuccessResponse(amenities, "Lấy danh sách tiện ích thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách tiện ích");
            return BadRequest(ApiResponse<List<AmenityDto>>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Lấy chi tiết 1 tiện ích (GET /api/amenities/{id})
    /// </summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AmenityDto>), 200)]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var amenity = await _amenityService.GetAmenityByIdAsync(id);
            return Ok(ApiResponse<AmenityDto>.SuccessResponse(amenity, "Lấy thông tin tiện ích thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy thông tin tiện ích ID: {Id}", id);
            return BadRequest(ApiResponse<AmenityDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Admin tạo tiện ích mới (POST /api/amenities)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<AmenityDto>), 200)]
    public async Task<IActionResult> Create([FromBody] CreateAmenityDto createDto)
    {
        try
        {
            var amenity = await _amenityService.CreateAmenityAsync(createDto);
            return Ok(ApiResponse<AmenityDto>.SuccessResponse(amenity, "Thêm tiện ích mới thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi thêm tiện ích mới");
            return BadRequest(ApiResponse<AmenityDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Admin cập nhật tiện ích (PUT /api/amenities/{id})
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<AmenityDto>), 200)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAmenityDto updateDto)
    {
        try
        {
            var amenity = await _amenityService.UpdateAmenityAsync(id, updateDto);
            return Ok(ApiResponse<AmenityDto>.SuccessResponse(amenity, "Cập nhật tiện ích thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật tiện ích ID: {Id}", id);
            return BadRequest(ApiResponse<AmenityDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Admin xóa tiện ích (DELETE /api/amenities/{id})
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _amenityService.DeleteAmenityAsync(id);
            return Ok(ApiResponse<object?>.SuccessResponse(null, "Xóa tiện ích thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa tiện ích ID: {Id}", id);
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }
}
