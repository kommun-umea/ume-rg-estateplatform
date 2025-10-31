using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Umea.se.EstateService.API;
using Umea.se.EstateService.ServiceAccess;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Models;
using Umea.se.EstateService.Test.TestHelpers;
using Umea.se.TestToolkit.TestInfrastructure;

namespace Umea.se.EstateService.Test.API;

public class FloorControllerTests : ControllerTestCloud<TestApiFactory, Program, HttpClientNames>
{
    private readonly HttpClient _client;
    private readonly FakePythagorasClient _fakeClient;

    public FloorControllerTests()
    {
        _client = Client;
        _client.DefaultRequestHeaders.Add("X-Api-Key", TestApiFactory.ApiKey);
        _fakeClient = WebAppFactory.FakeClient;

        MockManager.SetupUser(user => user.WithActualAuthorization());
    }

    [Fact]
    public async Task GetFloorAsync_WhenFound_ReturnsFloor()
    {
        _fakeClient.Reset();
        _fakeClient.SetGetAsyncResult(new Floor { Id = 1655, Name = "Test Floor", Uid = Guid.NewGuid() });

        HttpResponseMessage response = await _client.GetAsync($"{ApiRoutes.Floors}/1655");
        response.EnsureSuccessStatusCode();

        FloorInfoModel? floor = await response.Content.ReadFromJsonAsync<FloorInfoModel>();
        floor.ShouldNotBeNull();
        floor.Id.ShouldBe(1655);
        floor.Name.ShouldBe("Test Floor");

        string decodedQuery = Uri.UnescapeDataString(_fakeClient.LastQueryString ?? string.Empty);
        decodedQuery.ShouldContain("floorIds[]=1655");
        _fakeClient.LastEndpoint.ShouldBe("rest/v1/floor/info");
    }

    [Fact]
    public async Task GetFloorAsync_WhenMissing_Returns404()
    {
        _fakeClient.Reset();
        _fakeClient.SetGetAsyncResult(Array.Empty<Floor>());

        HttpResponseMessage response = await _client.GetAsync($"{ApiRoutes.Floors}/999");

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetFloorBlueprintAsync_ForPdf_ReturnsFileWithCacheHeaders()
    {
        _fakeClient.Reset();
        _fakeClient.OnGetFloorBlueprintAsync = (_, _, _, _) =>
        {
            byte[] pdfBytes = [0x25, 0x50, 0x44, 0x46]; // %PDF header
            HttpResponseMessage httpResponse = new(System.Net.HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(pdfBytes)
            };
            httpResponse.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
            return Task.FromResult(httpResponse);
        };

        HttpResponseMessage response = await _client.GetAsync($"{ApiRoutes.Floors}/10/blueprint?format=Pdf");
        response.EnsureSuccessStatusCode();

        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/pdf");
        response.Headers.CacheControl?.Public.ShouldBeTrue();
        response.Headers.CacheControl?.MaxAge.ShouldBe(TimeSpan.FromHours(24));
    }

    [Fact]
    public async Task GetFloorBlueprintAsync_ForSvg_ReturnsFileWithCacheHeaders()
    {
        _fakeClient.Reset();
        _fakeClient.OnGetFloorBlueprintAsync = (_, _, _, _) =>
        {
            byte[] svgBytes = "<svg></svg>"u8.ToArray();
            HttpResponseMessage httpResponse = new(System.Net.HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(svgBytes)
            };
            httpResponse.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/svg+xml");
            return Task.FromResult(httpResponse);
        };

        HttpResponseMessage response = await _client.GetAsync($"{ApiRoutes.Floors}/7/blueprint?format=Svg");
        response.EnsureSuccessStatusCode();

        response.Content.Headers.ContentType?.MediaType.ShouldBe("image/svg+xml");
        response.Headers.CacheControl?.Public.ShouldBeTrue();
        response.Headers.CacheControl?.MaxAge.ShouldBe(TimeSpan.FromHours(24));
    }

    [Fact]
    public async Task GetFloorBlueprintAsync_WhenServiceUnavailable_Returns502()
    {
        _fakeClient.Reset();
        _fakeClient.OnGetFloorBlueprintAsync = (_, _, _, _) =>
        {
            HttpResponseMessage httpResponse = new(System.Net.HttpStatusCode.ServiceUnavailable);
            return Task.FromResult(httpResponse);
        };

        HttpResponseMessage response = await _client.GetAsync($"{ApiRoutes.Floors}/12/blueprint?format=Pdf");

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task GetFloorBlueprintAsync_WhenNotFound_Returns404()
    {
        _fakeClient.Reset();
        _fakeClient.OnGetFloorBlueprintAsync = (_, _, _, _) =>
        {
            HttpResponseMessage httpResponse = new(System.Net.HttpStatusCode.NotFound);
            return Task.FromResult(httpResponse);
        };

        HttpResponseMessage response = await _client.GetAsync($"{ApiRoutes.Floors}/42/blueprint?format=Svg");

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.NotFound);
    }
}
