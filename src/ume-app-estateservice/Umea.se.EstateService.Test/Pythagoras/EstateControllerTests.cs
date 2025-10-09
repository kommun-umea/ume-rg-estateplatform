using System.Net.Http.Json;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Models;
using Umea.se.EstateService.API;
using Umea.se.EstateService.ServiceAccess;
using Umea.se.EstateService.Test.TestHelpers;
using Umea.se.TestToolkit.TestInfrastructure;

namespace Umea.se.EstateService.Test.Pythagoras;

public class EstateControllerTests : ControllerTestCloud<TestApiFactory, Program, HttpClientNames>
{
    private readonly HttpClient _client;
    private readonly FakePythagorasClient _fakeClient;

    public EstateControllerTests()
    {
        _client = Client;
        _client.DefaultRequestHeaders.Add("X-Api-Key", TestApiFactory.ApiKey);
        _fakeClient = WebAppFactory.FakeClient;

        MockManager.SetupUser(user => user.WithSsNo("1234567890"));
    }

    [Fact]
    public async Task GetEstateBuildingsAsync_ReturnsFilteredBuildingsByEstateId()
    {
        _fakeClient.Reset();
        _fakeClient.SetGetAsyncResult(
            new BuildingInfo { Id = 10, Name = "Building A" },
            new BuildingInfo { Id = 11, Name = "Building B" });

        HttpResponseMessage response = await _client.GetAsync($"{ApiRoutes.Estates}/123/buildings");
        response.EnsureSuccessStatusCode();

        IReadOnlyList<BuildingInfoModel>? buildings = await response.Content.ReadFromJsonAsync<IReadOnlyList<BuildingInfoModel>>();
        buildings.ShouldNotBeNull();

        buildings.Count.ShouldBe(2);
        buildings.Select(b => b.Id).ShouldBe([10, 11]);
        string decodedQuery = Uri.UnescapeDataString(_fakeClient.LastQueryString ?? string.Empty);
        decodedQuery.ShouldContain("navigationFolderId=123");
        _fakeClient.LastEndpoint.ShouldBe("rest/v1/building/info");
    }

    [Fact]
    public async Task GetEstateBuildingsAsync_WithZeroResults_ReturnsEmptyList()
    {
        _fakeClient.Reset();
        _fakeClient.SetGetAsyncResult(Array.Empty<BuildingInfo>());

        HttpResponseMessage response = await _client.GetAsync($"{ApiRoutes.Estates}/456/buildings");
        response.EnsureSuccessStatusCode();

        IReadOnlyList<BuildingInfoModel>? buildings = await response.Content.ReadFromJsonAsync<IReadOnlyList<BuildingInfoModel>>();
        buildings.ShouldNotBeNull();

        buildings.ShouldBeEmpty();
        string decodedQuery = Uri.UnescapeDataString(_fakeClient.LastQueryString ?? string.Empty);
        decodedQuery.ShouldContain("navigationFolderId=456");
        _fakeClient.LastEndpoint.ShouldBe("rest/v1/building/info");
    }

}
