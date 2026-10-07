using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using RoomRental.BackEnd.DTO.Location;

namespace RoomRental.BackEnd.Services;

public sealed class NominatimGeocodingService : IDisposable
{
    private readonly IHttpClientFactory _clients;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly MemoryCache _cache = new(new MemoryCacheOptions { SizeLimit = 512 });
    private long _lastRequest;

    public NominatimGeocodingService(IHttpClientFactory clients) => _clients = clients;

    public async Task<List<LocationSearchResultDto>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        var json = await GetAsync($"search?format=jsonv2&q={Uri.EscapeDataString(query.Trim())}&limit=5&countrycodes=vn&addressdetails=1&accept-language=vi", cancellationToken);
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Array) throw new JsonException("Unexpected geocoding search response.");
        return document.RootElement.EnumerateArray().Select(ParseLocation).OfType<LocationSearchResultDto>().ToList();
    }

    public async Task<LocationSearchResultDto?> ReverseAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        var lat = latitude.ToString("R", CultureInfo.InvariantCulture);
        var lng = longitude.ToString("R", CultureInfo.InvariantCulture);
        var json = await GetAsync($"reverse?format=jsonv2&lat={lat}&lon={lng}&zoom=18&addressdetails=1&accept-language=vi", cancellationToken);
        using var document = JsonDocument.Parse(json);
        var result = ParseLocation(document.RootElement);
        if (result != null)
        {
            // Preserve the clicked pin, rather than the nearest OSM object's coordinates.
            result.Latitude = latitude;
            result.Longitude = longitude;
        }
        return result;
    }

    private async Task<string> GetAsync(string path, CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(path, out string? cached)) return cached!;
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_cache.TryGetValue(path, out cached)) return cached!;
            // The limit applies to all browsers sharing this application instance.
            if (_lastRequest != 0)
            {
                var remaining = TimeSpan.FromMilliseconds(1100) - Stopwatch.GetElapsedTime(_lastRequest);
                if (remaining > TimeSpan.Zero) await Task.Delay(remaining, cancellationToken);
            }
            _lastRequest = Stopwatch.GetTimestamp();
            using var response = await _clients.CreateClient("Geocoding").GetAsync(path, cancellationToken);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(json);
            _cache.Set(path, json, new MemoryCacheEntryOptions { Size = 1, AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6) });
            return json;
        }
        finally { _gate.Release(); }
    }

    private static LocationSearchResultDto? ParseLocation(JsonElement item)
    {
        if (item.ValueKind != JsonValueKind.Object || !double.TryParse(Text(item, "lat"), NumberStyles.Float, CultureInfo.InvariantCulture, out var lat)
            || !double.TryParse(Text(item, "lon"), NumberStyles.Float, CultureInfo.InvariantCulture, out var lng)
            || !double.IsFinite(lat) || !double.IsFinite(lng) || lat is < -90 or > 90 || lng is < -180 or > 180) return null;
        var displayName = Text(item, "display_name");
        var details = item.TryGetProperty("address", out var address) ? address : default;
        var province = Text(details, "state", "province", "city");
        var district = Text(details, "city_district", "district", "county");
        var city = Text(details, "city");
        if (district.Length == 0 && !string.Equals(city, province, StringComparison.OrdinalIgnoreCase)) district = city;
        var street = Text(details, "road", "pedestrian", "residential", "path");
        var specificAddress = string.Join(" ", new[] { Text(details, "house_number"), street }.Where(s => s.Length > 0));
        return new LocationSearchResultDto
        {
            Latitude = lat, Longitude = lng, DisplayName = displayName,
            Province = province, District = district,
            Ward = Text(details, "ward", "suburb", "quarter", "village", "town"),
            Address = specificAddress.Length > 0 ? specificAddress : displayName
        };
    }

    private static string Text(JsonElement item, params string[] keys)
    {
        if (item.ValueKind != JsonValueKind.Object) return string.Empty;
        foreach (var key in keys)
            if (item.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString()))
                return value.GetString()!.Trim();
        return string.Empty;
    }

    public void Dispose() { _cache.Dispose(); _gate.Dispose(); }
}
