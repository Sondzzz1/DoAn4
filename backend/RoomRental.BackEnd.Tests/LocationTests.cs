using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using RoomRental.BackEnd.Controllers;
using RoomRental.BackEnd.Services;
using Xunit;

namespace RoomRental.BackEnd.Tests;

public sealed class LocationTests
{
    private const string LocationJson = """
        {"lat":"21.02851","lon":"105.85421","display_name":"42 Nguyen Lan, Thanh Xuan, Ha Noi",
        "address":{"house_number":"42","road":"Nguyen Lan","suburb":"Phuong Mai","city_district":"Thanh Xuan","city":"Ha Noi"}}
        """;

    [Fact]
    public async Task Reverse_preserves_exact_pin_and_parses_address_parts()
    {
        using var factory = new FakeFactory(LocationJson);
        using var service = new NominatimGeocodingService(factory);
        var result = await service.ReverseAsync(21.02, 105.85, default);
        Assert.NotNull(result);
        Assert.Equal(21.02, result.Latitude);
        Assert.Equal(105.85, result.Longitude);
        Assert.Equal("42 Nguyen Lan", result.Address);
        Assert.Equal("Ha Noi", result.Province);
        Assert.Equal("Thanh Xuan", result.District);
        Assert.Equal("Phuong Mai", result.Ward);
        Assert.Contains("addressdetails=1", factory.Paths.Single());
    }

    [Fact]
    public async Task Search_uses_invariant_decimal_parsing_and_cached_results()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("vi-VN");
        try
        {
            using var factory = new FakeFactory("[" + LocationJson + "]");
            using var service = new NominatimGeocodingService(factory);
            var results = await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => service.SearchAsync("42 Nguyen Lan", default)));
            Assert.All(results, list => Assert.Equal(21.02851, Assert.Single(list).Latitude));
            Assert.Single(factory.Paths);
            Assert.Contains("countrycodes=vn", factory.Paths.Single());
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Fact]
    public async Task Reverse_without_coverage_returns_null()
    {
        using var factory = new FakeFactory("{\"error\":\"Unable to geocode\"}");
        using var service = new NominatimGeocodingService(factory);
        Assert.Null(await service.ReverseAsync(0, 0, default));
    }

    [Fact]
    public async Task Search_skips_invalid_coordinates()
    {
        using var factory = new FakeFactory("[{\"lat\":\"NaN\",\"lon\":\"105\"},{\"lat\":\"91\",\"lon\":\"105\"}]");
        using var service = new NominatimGeocodingService(factory);
        Assert.Empty(await service.SearchAsync("Invalid", default));
    }

    [Fact]
    public async Task Search_and_reverse_share_provider_request_limit()
    {
        using var factory = new FakeFactory(LocationJson);
        using var service = new NominatimGeocodingService(factory);
        await Task.WhenAll(service.ReverseAsync(21, 105, default), service.ReverseAsync(22, 106, default));
        Assert.Equal(2, factory.RequestTimes.Count);
        Assert.True(factory.RequestTimes[1] - factory.RequestTimes[0] >= TimeSpan.FromMilliseconds(1000));
    }

    [Theory]
    [InlineData(91, 105)]
    [InlineData(21, 181)]
    [InlineData(double.NaN, 105)]
    public async Task Controller_rejects_invalid_coordinates_without_calling_provider(double lat, double lng)
    {
        using var factory = new FakeFactory(LocationJson);
        using var service = new NominatimGeocodingService(factory);
        var controller = Controller(service);
        Assert.IsType<BadRequestObjectResult>(await controller.ReverseLocation(lat, lng, default));
        Assert.Empty(factory.Paths);
    }

    [Fact]
    public async Task Controller_rejects_missing_coordinates_and_blank_query()
    {
        using var factory = new FakeFactory(LocationJson);
        using var service = new NominatimGeocodingService(factory);
        var controller = Controller(service);
        controller.Request.QueryString = QueryString.Empty;
        Assert.IsType<BadRequestObjectResult>(await controller.ReverseLocation(0, 0, default));
        Assert.IsType<BadRequestObjectResult>(await controller.SearchLocation(" ", default));
        Assert.Empty(factory.Paths);
    }

    [Fact]
    public async Task Controller_maps_upstream_failure_to_503()
    {
        using var factory = new FakeFactory("{}", HttpStatusCode.ServiceUnavailable);
        using var service = new NominatimGeocodingService(factory);
        Assert.Equal(503, Assert.IsType<ObjectResult>(await Controller(service).ReverseLocation(21, 105, default)).StatusCode);
    }

    [Fact]
    public async Task Cancelled_request_does_not_call_provider()
    {
        using var factory = new FakeFactory(LocationJson);
        using var service = new NominatimGeocodingService(factory);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.SearchAsync("Test", cancellation.Token));
        Assert.Empty(factory.Paths);
    }

    private static LocationController Controller(NominatimGeocodingService service)
    {
        var controller = new LocationController(service, NullLogger<LocationController>.Instance)
        { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        controller.Request.QueryString = new QueryString("?lat=21&lng=105");
        return controller;
    }

    private sealed class FakeFactory : HttpMessageHandler, IHttpClientFactory
    {
        private readonly string _json;
        private readonly HttpStatusCode _status;
        private readonly HttpClient _client;
        public List<string> Paths { get; } = new();
        public List<DateTimeOffset> RequestTimes { get; } = new();
        public FakeFactory(string json, HttpStatusCode status = HttpStatusCode.OK)
        {
            _json = json; _status = status;
            _client = new HttpClient(this, disposeHandler: false) { BaseAddress = new Uri("https://geocoding.example.test/") };
        }
        public HttpClient CreateClient(string name) => _client;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Paths.Add(request.RequestUri!.PathAndQuery);
            RequestTimes.Add(DateTimeOffset.UtcNow);
            return Task.FromResult(new HttpResponseMessage(_status) { Content = new StringContent(_json) });
        }
        protected override void Dispose(bool disposing) { if (disposing) _client.Dispose(); base.Dispose(disposing); }
    }
}
