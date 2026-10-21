using System.Globalization;
using System.Net;
using System.Text.Json;
using RoomRental.BackEnd.Services;
using Xunit;

namespace RoomRental.BackEnd.Tests;

public class GeocodingFallbackTests
{
    private const string Photon = """
        {"features":[{"properties":{"name":"Nhà trọ thử","street":"Nguyễn Lân","housenumber":"42",
        "district":"Phương Liệt","county":"Thanh Xuân","state":"Hà Nội","country":"Việt Nam","countrycode":"VN"},
        "geometry":{"type":"Point","coordinates":[105.84,21.03]}}]}
        """;

    [Theory]
    [InlineData("dns")]
    [InlineData("timeout")]
    [InlineData("json")]
    public async Task Primary_outage_uses_fallback_with_the_existing_search_contract(string failure)
    {
        using var factory = new Factory(failure);
        using var service = new NominatimGeocodingService(factory);
        var result = Assert.Single(await service.SearchAsync("  42 Nguyễn Lân  ", default));
        Assert.Equal(21.03, result.Latitude); Assert.Equal(105.84, result.Longitude);
        Assert.Equal("42 Nguyễn Lân", result.Address); Assert.Equal("Hà Nội", result.Province);
        Assert.Equal("Thanh Xuân", result.District); Assert.Equal("Phương Liệt", result.Ward);
        Assert.Contains("countrycode=VN", factory.Calls[1].Path);
        Assert.Contains(Uri.EscapeDataString("42 Nguyễn Lân"), factory.Calls[1].Path);
        Assert.Equal(new[] { "Geocoding", "GeocodingFallback" }, factory.Calls.Select(c => c.Client));
    }

    [Fact]
    public async Task Healthy_primary_does_not_call_a_second_provider()
    {
        using var factory = new Factory("healthy");
        using var service = new NominatimGeocodingService(factory);
        Assert.Single(await service.SearchAsync("Hà Nội", default));
        Assert.Equal("Geocoding", Assert.Single(factory.Calls).Client);
    }

    [Fact]
    public async Task Reverse_fallback_preserves_exact_clicked_pin_with_Vietnamese_culture()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("vi-VN");
        try
        {
            using var factory = new Factory("dns"); using var service = new NominatimGeocodingService(factory);
            var result = await service.ReverseAsync(21.030123, 105.840456, default);
            Assert.NotNull(result); Assert.Equal(21.030123, result.Latitude); Assert.Equal(105.840456, result.Longitude);
            Assert.Equal("42 Nguyễn Lân", result.Address);
            Assert.Contains("lat=21.030123&lon=105.840456", factory.Calls[1].Path);
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Fact]
    public async Task Fallback_results_are_cached_without_repeating_the_public_provider_request()
    {
        using var factory = new Factory("dns"); using var service = new NominatimGeocodingService(factory);
        await service.SearchAsync("Hà Nội", default); await service.SearchAsync("Hà Nội", default);
        Assert.Single(factory.Calls, c => c.Client == "GeocodingFallback");
    }

    [Theory]
    [InlineData("{\"features\":[]}")]
    [InlineData("{\"features\":[null,{},42]}")]
    [InlineData("{\"features\":[{\"properties\":{\"name\":\"Tokyo\",\"countrycode\":\"JP\"},\"geometry\":{\"type\":\"Point\",\"coordinates\":[139,35]}}]}")]
    [InlineData("{\"features\":[{\"properties\":{\"name\":\"Invalid\",\"countrycode\":\"VN\"},\"geometry\":{\"type\":\"Point\",\"coordinates\":[105,91]}}]}")]
    [InlineData("{\"features\":[{\"properties\":{\"name\":\"Invalid\",\"countrycode\":\"VN\"},\"geometry\":{\"type\":\"Point\",\"coordinates\":[\"105\",21]}}]}")]
    public async Task Fallback_does_not_invent_locations_for_empty_invalid_or_foreign_results(string json)
    {
        using var factory = new Factory("dns", json); using var service = new NominatimGeocodingService(factory);
        Assert.Empty(await service.SearchAsync("Địa chỉ thử", default));
    }

    [Fact]
    public async Task Cancellation_does_not_start_a_fallback_lookup()
    {
        using var cancellation = new CancellationTokenSource();
        using var factory = new Factory("cancel", cancel: cancellation.Cancel);
        using var service = new NominatimGeocodingService(factory);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.SearchAsync("Hà Nội", cancellation.Token));
        Assert.Equal("Geocoding", Assert.Single(factory.Calls).Client);
    }

    [Fact]
    public async Task Both_providers_unavailable_still_report_an_error_not_a_fake_address()
    {
        using var factory = new Factory("dns", fallbackStatus: HttpStatusCode.ServiceUnavailable);
        using var service = new NominatimGeocodingService(factory);
        await Assert.ThrowsAsync<HttpRequestException>(() => service.SearchAsync("Hà Nội", default));
        Assert.Equal(2, factory.Calls.Count);
    }

    // Fictional address fixtures; provider responses are mocked.
    private static object Feature(string house, string street, string type = "house", string province = "Thành phố Mẫu") => new
    {
        properties = new { name = street, street, housenumber = house, type, district = "Phường Mẫu", city = province, country = "Việt Nam", countrycode = "VN" },
        geometry = new { type = "Point", coordinates = new[] { 105.81, 21.01 } }
    };

    [Fact]
    public async Task Google_formatted_address_uses_structured_house_street_ward_city_not_fuzzy_district_search()
    {
        var json = JsonSerializer.Serialize(new { features = new[] { Feature("9999", "Phố Đối Chiếu"), Feature("8888", "Phố Kiểm Thử"), Feature("", "Phố Kiểm Thử", "street") } });
        using var factory = new Factory("dns", json); using var service = new NominatimGeocodingService(factory);
        var result = Assert.Single(await service.SearchAsync("9999 P. Kiểm Thử, Phường Mẫu, Thành phố Mẫu 100000, Việt Nam", default));
        Assert.Equal("Phố Kiểm Thử", result.Street); Assert.Equal("", result.HouseNumber); Assert.Equal("street", result.MatchType);
        var path = factory.Calls[1].Path;
        Assert.StartsWith("/structured?", path); Assert.Contains("housenumber=9999", path);
        Assert.Contains("street=" + Uri.EscapeDataString("Kiểm Thử"), path);
        Assert.Contains("city=" + Uri.EscapeDataString("Mẫu"), path);
        Assert.Contains("district=" + Uri.EscapeDataString("Mẫu"), path); Assert.DoesNotContain("100000", path);
    }

    [Fact]
    public async Task Exact_house_is_preferred_over_street_and_different_house_results()
    {
        var json = JsonSerializer.Serialize(new { features = new[] { Feature("", "Phố Kiểm Thử", "street"), Feature("8888", "Phố Kiểm Thử"), Feature("9999", "Phố Kiểm Thử") } });
        using var factory = new Factory("dns", json); using var service = new NominatimGeocodingService(factory);
        var result = Assert.Single(await service.SearchAsync("9999 Phố Kiểm Thử, Phường Mẫu, Thành phố Mẫu, Việt Nam", default));
        Assert.Equal("9999", result.HouseNumber); Assert.Equal("address", result.MatchType);
    }

    [Theory]
    [InlineData("9999", "Phố Đối Chiếu", "Thành phố Mẫu")]
    [InlineData("8888", "Phố Kiểm Thử", "Thành phố Mẫu")]
    [InlineData("9999", "Phố Kiểm Thử", "Hưng Yên")]
    public async Task Different_street_house_or_province_is_not_claimed_as_the_requested_house(string house, string street, string province)
    {
        var json = JsonSerializer.Serialize(new { features = new[] { Feature(house, street, province: province) } });
        using var factory = new Factory("dns", json); using var service = new NominatimGeocodingService(factory);
        Assert.Empty(await service.SearchAsync("9999 Phố Kiểm Thử, Phường Mẫu, Thành phố Mẫu, Việt Nam", default));
    }

    [Fact]
    public async Task Primary_fuzzy_results_without_the_requested_street_try_the_structured_fallback()
    {
        var json = JsonSerializer.Serialize(new { features = new[] { Feature("", "Phố Kiểm Thử", "street") } });
        using var factory = new Factory("healthy", json); using var service = new NominatimGeocodingService(factory);
        var result = Assert.Single(await service.SearchAsync("9999 Phố Kiểm Thử, Phường Mẫu, Thành phố Mẫu, Việt Nam", default));
        Assert.Equal("street", result.MatchType); Assert.Equal(2, factory.Calls.Count);
    }

    private sealed class Factory : IHttpClientFactory, IDisposable
    {
        public List<(string Client, string Path)> Calls { get; } = [];
        private readonly Dictionary<string, HttpClient> _clients;
        public Factory(string failure, string photon = Photon, HttpStatusCode fallbackStatus = HttpStatusCode.OK, Action? cancel = null)
        {
            _clients = new[] { "Geocoding", "GeocodingFallback" }.ToDictionary(name => name, name =>
                new HttpClient(new Handler((request, token) =>
                {
                    Calls.Add((name, request.RequestUri!.PathAndQuery));
                    if (name == "GeocodingFallback") return Response(photon, fallbackStatus);
                    return failure switch
                    {
                        "healthy" => Response("[{\"lat\":\"21.03\",\"lon\":\"105.84\",\"display_name\":\"Hà Nội\"}]"),
                        "json" => Response("Not JSON"),
                        "timeout" => Task.FromException<HttpResponseMessage>(new TaskCanceledException("Provider timed out")),
                        "cancel" => Cancel(),
                        _ => Task.FromException<HttpResponseMessage>(new HttpRequestException("Primary DNS unavailable"))
                    };
                    Task<HttpResponseMessage> Cancel()
                    {
                        cancel!();
                        return Task.FromException<HttpResponseMessage>(new OperationCanceledException(token));
                    }
                })) { BaseAddress = new Uri("https://provider.example.test/") });
        }
        private static Task<HttpResponseMessage> Response(string json, HttpStatusCode status = HttpStatusCode.OK) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(json) });
        public HttpClient CreateClient(string name) => _clients[name];
        public void Dispose() { foreach (var client in _clients.Values) client.Dispose(); }
    }

    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request, cancellationToken);
    }
}
