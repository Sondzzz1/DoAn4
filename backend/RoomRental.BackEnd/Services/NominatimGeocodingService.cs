using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using RoomRental.BackEnd.DTO.Location;

namespace RoomRental.BackEnd.Services;

public sealed class NominatimGeocodingService : IDisposable
{
    private readonly IHttpClientFactory _clients;
    private readonly ILogger<NominatimGeocodingService>? _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly MemoryCache _cache = new(new MemoryCacheOptions { SizeLimit = 512 });
    private long _lastRequest;

    public NominatimGeocodingService(IHttpClientFactory clients, ILogger<NominatimGeocodingService>? logger = null)
    {
        _clients = clients;
        _logger = logger;
    }

    public async Task<List<LocationSearchResultDto>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        var address = GeocodingAddressQuery.Parse(query);
        try
        {
            var json = await GetAsync($"search?format=jsonv2&q={Uri.EscapeDataString(query.Trim())}&limit=5&countrycodes=vn&addressdetails=1&accept-language=vi", cancellationToken);
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array) throw new JsonException("Unexpected geocoding search response.");
            var results = document.RootElement.EnumerateArray().Select(ParseLocation).OfType<LocationSearchResultDto>().ToList();
            if (address == null) return results;
            var matching = address.Relevant(results);
            if (matching.Count > 0) return matching;
        }
        catch (Exception error) when (CanUseFallback(error, cancellationToken))
        {
            _logger?.LogWarning(error, "Primary geocoding search unavailable; trying the fallback provider.");
        }
        var fallback = await PhotonAsync(address?.PhotonPath() ?? $"api/?q={Uri.EscapeDataString(query.Trim())}&limit=5&countrycode=VN", cancellationToken);
        return address?.Relevant(fallback) ?? fallback;
    }

    public async Task<LocationSearchResultDto?> ReverseAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        var lat = latitude.ToString("R", CultureInfo.InvariantCulture);
        var lng = longitude.ToString("R", CultureInfo.InvariantCulture);
        LocationSearchResultDto? result;
        try
        {
            var json = await GetAsync($"reverse?format=jsonv2&lat={lat}&lon={lng}&zoom=18&addressdetails=1&accept-language=vi", cancellationToken);
            using var document = JsonDocument.Parse(json);
            result = ParseLocation(document.RootElement);
        }
        catch (Exception error) when (CanUseFallback(error, cancellationToken))
        {
            _logger?.LogWarning(error, "Primary reverse geocoding unavailable; trying the fallback provider.");
            result = (await PhotonAsync($"reverse?lat={lat}&lon={lng}&limit=1", cancellationToken)).FirstOrDefault();
        }
        if (result != null)
        {
            // Preserve the clicked pin, rather than the nearest OSM object's coordinates.
            result.Latitude = latitude;
            result.Longitude = longitude;
        }
        return result;
    }

    private static bool CanUseFallback(Exception error, CancellationToken cancellationToken) =>
        !cancellationToken.IsCancellationRequested && error is HttpRequestException or JsonException or OperationCanceledException;

    private async Task<List<LocationSearchResultDto>> PhotonAsync(string path, CancellationToken cancellationToken)
    {
        var json = await GetAsync(path, cancellationToken, "GeocodingFallback");
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object || !document.RootElement.TryGetProperty("features", out var features)
            || features.ValueKind != JsonValueKind.Array) throw new JsonException("Unexpected fallback geocoding response.");
        return features.EnumerateArray().Select(ParsePhotonLocation).OfType<LocationSearchResultDto>().ToList();
    }

    private static LocationSearchResultDto? ParsePhotonLocation(JsonElement feature)
    {
        if (feature.ValueKind != JsonValueKind.Object || !feature.TryGetProperty("properties", out var properties)
            || !string.Equals(Text(properties, "countrycode"), "VN", StringComparison.OrdinalIgnoreCase)
            || !feature.TryGetProperty("geometry", out var geometry) || geometry.ValueKind != JsonValueKind.Object
            || Text(geometry, "type") != "Point" || !geometry.TryGetProperty("coordinates", out var point)
            || point.ValueKind != JsonValueKind.Array || point.GetArrayLength() < 2
            || point[0].ValueKind != JsonValueKind.Number || point[1].ValueKind != JsonValueKind.Number
            || !point[0].TryGetDouble(out var lng) || !point[1].TryGetDouble(out var lat)
            || !double.IsFinite(lat) || !double.IsFinite(lng) || lat is < -90 or > 90 || lng is < -180 or > 180) return null;
        var name = Text(properties, "name");
        var kind = Text(properties, "type");
        var streetName = Text(properties, "street");
        if (streetName.Length == 0 && kind == "street") streetName = name;
        var houseNumber = Text(properties, "housenumber");
        var street = string.Join(" ", new[] { houseNumber, streetName }.Where(s => s.Length > 0));
        var city = Text(properties, "city");
        var province = Text(properties, "state", "province");
        if (province.Length == 0) province = city;
        var district = Text(properties, "county");
        if (district.Length == 0 && !string.Equals(city, province, StringComparison.OrdinalIgnoreCase)) district = city;
        var ward = Text(properties, "district", "locality");
        var displayName = string.Join(", ", new[] { name, street, ward, district, province, Text(properties, "country") }
            .Where(s => s.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase));
        if (street.Length == 0 && name.Length == 0) return null;
        return new LocationSearchResultDto { Latitude = lat, Longitude = lng, DisplayName = displayName,
            Address = street.Length > 0 ? street : name, Province = province, District = district, Ward = ward,
            Street = streetName, HouseNumber = houseNumber,
            MatchType = houseNumber.Length > 0 && streetName.Length > 0 ? "address" : kind == "street" ? "street" : kind is "city" or "state" or "district" ? "area" : "place" };
    }

    private async Task<string> GetAsync(string path, CancellationToken cancellationToken, string clientName = "Geocoding")
    {
        var cacheKey = clientName + ":" + path;
        if (_cache.TryGetValue(cacheKey, out string? cached)) return cached!;
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_cache.TryGetValue(cacheKey, out cached)) return cached!;
            // The limit applies to all browsers sharing this application instance.
            if (_lastRequest != 0)
            {
                var remaining = TimeSpan.FromMilliseconds(1100) - Stopwatch.GetElapsedTime(_lastRequest);
                if (remaining > TimeSpan.Zero) await Task.Delay(remaining, cancellationToken);
            }
            _lastRequest = Stopwatch.GetTimestamp();
            using var response = await _clients.CreateClient(clientName).GetAsync(path, cancellationToken);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(json);
            _cache.Set(cacheKey, json, new MemoryCacheEntryOptions { Size = 1, AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6) });
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
        var houseNumber = Text(details, "house_number");
        var specificAddress = string.Join(" ", new[] { houseNumber, street }.Where(s => s.Length > 0));
        return new LocationSearchResultDto
        {
            Latitude = lat, Longitude = lng, DisplayName = displayName,
            Province = province, District = district,
            Ward = Text(details, "ward", "suburb", "quarter", "village", "town"),
            Address = specificAddress.Length > 0 ? specificAddress : displayName,
            Street = street, HouseNumber = houseNumber,
            MatchType = houseNumber.Length > 0 && street.Length > 0 ? "address" : Text(item, "addresstype") is "road" or "street" ? "street" : "place"
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
