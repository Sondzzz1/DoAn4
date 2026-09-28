using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomRental.BackEnd.DTO.Common;
using RoomRental.BackEnd.DTO.Location;
using System.Text.Json;

namespace RoomRental.BackEnd.Controllers;

/// <summary>
/// Controller proxy cho Nominatim Location Search
/// </summary>
[Route("api/location")]
[ApiController]
public class LocationController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<LocationController> _logger;

    public LocationController(IHttpClientFactory httpClientFactory, ILogger<LocationController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Tìm kiếm địa điểm qua Nominatim (proxy để tránh CORS/DNS issues)
    /// GET /api/location/search?q=42+Đường+Nguyễn+Lân
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<LocationSearchResultDto>>), 200)]
    public async Task<IActionResult> SearchLocation([FromQuery] string q)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return BadRequest(ApiResponse<List<LocationSearchResultDto>>.ErrorResponse("Vui lòng nhập địa chỉ tìm kiếm"));
            }

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "RoomRentalSystem/1.0");

            var nominatimUrl = $"https://nominatim.openstreetmap.org/search?format=json&q={Uri.EscapeDataString(q)}&limit=5&countrycodes=vn";

            _logger.LogInformation("Calling Nominatim: {Url}", nominatimUrl);

            var response = await httpClient.GetAsync(nominatimUrl);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var nominatimResults = JsonSerializer.Deserialize<List<NominatimResult>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (nominatimResults == null || nominatimResults.Count == 0)
            {
                return Ok(ApiResponse<List<LocationSearchResultDto>>.SuccessResponse(
                    new List<LocationSearchResultDto>(), 
                    "Không tìm thấy kết quả"));
            }

            var results = nominatimResults.Select(r => new LocationSearchResultDto
            {
                DisplayName = r.DisplayName ?? r.Display_Name ?? "",
                Latitude = double.TryParse(r.Lat, out var lat) ? lat : 0,
                Longitude = double.TryParse(r.Lon, out var lon) ? lon : 0
            }).ToList();

            _logger.LogInformation("Found {Count} results", results.Count);

            return Ok(ApiResponse<List<LocationSearchResultDto>>.SuccessResponse(results, "Tìm kiếm thành công"));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling Nominatim");
            return StatusCode(503, ApiResponse<List<LocationSearchResultDto>>.ErrorResponse("Không thể kết nối đến dịch vụ bản đồ. Vui lòng thử lại sau."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching location");
            return BadRequest(ApiResponse<List<LocationSearchResultDto>>.ErrorResponse($"Lỗi tìm kiếm: {ex.Message}"));
        }
    }

    // Internal DTO for Nominatim response
    private class NominatimResult
    {
        public string? Lat { get; set; }
        public string? Lon { get; set; }
        public string? DisplayName { get; set; }
        public string? Display_Name { get; set; } // Nominatim uses snake_case
    }
}
