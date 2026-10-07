using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomRental.BackEnd.DTO.Common;
using RoomRental.BackEnd.DTO.Location;
using RoomRental.BackEnd.Services;
using System.Text.Json;

namespace RoomRental.BackEnd.Controllers;

[Route("api/location")]
[ApiController]
[AllowAnonymous]
public class LocationController : ControllerBase
{
    private readonly NominatimGeocodingService _geocoding;
    private readonly ILogger<LocationController> _logger;

    public LocationController(NominatimGeocodingService geocoding, ILogger<LocationController> logger)
    {
        _geocoding = geocoding;
        _logger = logger;
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<List<LocationSearchResultDto>>), 200)]
    public async Task<IActionResult> SearchLocation([FromQuery] string q, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length > 500)
            return BadRequest(ApiResponse<List<LocationSearchResultDto>>.ErrorResponse("Địa chỉ tìm kiếm phải có từ 1 đến 500 ký tự."));
        try
        {
            var results = await _geocoding.SearchAsync(q, cancellationToken);
            return Ok(ApiResponse<List<LocationSearchResultDto>>.SuccessResponse(results, results.Count > 0 ? "Tìm kiếm thành công" : "Không tìm thấy kết quả"));
        }
        catch (Exception ex) when (IsUpstreamError(ex, cancellationToken))
        {
            _logger.LogWarning(ex, "Geocoding search unavailable");
            return StatusCode(503, ApiResponse<List<LocationSearchResultDto>>.ErrorResponse("Không thể tra cứu địa chỉ lúc này. Vui lòng thử lại sau."));
        }
    }

    [HttpGet("reverse")]
    [ProducesResponseType(typeof(ApiResponse<LocationSearchResultDto>), 200)]
    public async Task<IActionResult> ReverseLocation([FromQuery] double lat, [FromQuery] double lng, CancellationToken cancellationToken)
    {
        if (!Request.Query.ContainsKey("lat") || !Request.Query.ContainsKey("lng") || !double.IsFinite(lat) || !double.IsFinite(lng)
            || lat is < -90 or > 90 || lng is < -180 or > 180)
            return BadRequest(ApiResponse<LocationSearchResultDto>.ErrorResponse("Tọa độ không hợp lệ."));
        try
        {
            var result = await _geocoding.ReverseAsync(lat, lng, cancellationToken);
            if (result == null || string.IsNullOrWhiteSpace(result.DisplayName))
                return NotFound(ApiResponse<LocationSearchResultDto>.ErrorResponse("Không tìm thấy địa chỉ tại vị trí đã chọn."));
            return Ok(ApiResponse<LocationSearchResultDto>.SuccessResponse(result, "Tra cứu địa chỉ thành công"));
        }
        catch (Exception ex) when (IsUpstreamError(ex, cancellationToken))
        {
            _logger.LogWarning(ex, "Reverse geocoding unavailable");
            return StatusCode(503, ApiResponse<LocationSearchResultDto>.ErrorResponse("Không thể tra cứu địa chỉ lúc này. Vui lòng thử lại sau."));
        }
    }

    private static bool IsUpstreamError(Exception error, CancellationToken cancellationToken) =>
        error is HttpRequestException or JsonException || (error is OperationCanceledException && !cancellationToken.IsCancellationRequested);
}
