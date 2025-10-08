using System.Net.Http.Json;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Models;
using Umea.se.EstateService.Test.TestHelpers;
using Umea.se.EstateService.API;

namespace Umea.se.EstateService.Test.Pythagoras;

public class BuildingControllerTests : IClassFixture<TestApiFactory>
{
    private readonly HttpClient _client;
    private readonly FakePythagorasClient _fakeClient;

    public BuildingControllerTests(TestApiFactory factory)
    {
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Api-Key", TestApiFactory.ApiKey);
        _fakeClient = factory.FakeClient;
    }

    [Fact]
    public async Task GetBuildingsAsync_ReturnsFirst50Buildings()
    {
        _fakeClient.Reset();
        _fakeClient.SetGetAsyncResult(
            new BuildingInfo { Id = 1, Name = "One" },
            new BuildingInfo { Id = 2, Name = "Two" });

        HttpResponseMessage response = await _client.GetAsync(ApiRoutes.Buildings);
        response.EnsureSuccessStatusCode();

        IReadOnlyList<BuildingInfoModel>? buildings = await response.Content.ReadFromJsonAsync<IReadOnlyList<BuildingInfoModel>>();
        buildings.ShouldNotBeNull();

        buildings.Select(b => b.Id).ShouldBe([1, 2]);
        _fakeClient.LastQueryString.ShouldBe("maxResults=50");
        _fakeClient.LastEndpoint.ShouldBe("rest/v1/building/info");
    }

    [Fact]
    public async Task GetBuildingsAsync_WithSearchTerm_FiltersByNameContains()
    {
        _fakeClient.Reset();
        _fakeClient.SetGetAsyncResult(new BuildingInfo { Id = 3, Name = "Alpha" });

        HttpResponseMessage response = await _client.GetAsync($"{ApiRoutes.Buildings}?searchTerm=alp");
        response.EnsureSuccessStatusCode();

        IReadOnlyList<BuildingInfoModel>? buildings = await response.Content.ReadFromJsonAsync<IReadOnlyList<BuildingInfoModel>>();
        buildings.ShouldNotBeNull();

        BuildingInfoModel building = buildings.ShouldHaveSingleItem();
        building.Name.ShouldBe("Alpha");

        string decodedQuery = Uri.UnescapeDataString(_fakeClient.LastQueryString ?? string.Empty);
        decodedQuery.ShouldContain("generalSearch=alp");
        decodedQuery.ShouldContain("maxResults=50");
        _fakeClient.LastEndpoint.ShouldBe("rest/v1/building/info");
    }

    [Fact]
    public async Task GetBuildingRoomsAsync_ReturnsMappedRooms()
    {
        _fakeClient.Reset();
        _fakeClient.SetGetAsyncResult(
            new BuildingWorkspace { Id = 10, BuildingId = 1, BuildingName = "B" });

        HttpResponseMessage response = await _client.GetAsync($"{ApiRoutes.Buildings}/1/rooms");
        response.EnsureSuccessStatusCode();

        IReadOnlyList<BuildingRoomModel>? result = await response.Content.ReadFromJsonAsync<IReadOnlyList<BuildingRoomModel>>();
        result.ShouldNotBeNull();

        BuildingRoomModel room = result.ShouldHaveSingleItem();
        room.Id.ShouldBe(10);
        _fakeClient.LastQueryString.ShouldBe("maxResults=50");
        _fakeClient.LastEndpoint.ShouldBe("rest/v1/building/1/workspace/info");
    }

    [Fact]
    public async Task GetBuildingFloorsAsync_AppliesQueryParameters()
    {
        _fakeClient.Reset();
        _fakeClient.SetGetAsyncResult(new Floor { Id = 5, Uid = Guid.NewGuid(), Name = "Floor 1" });
        _fakeClient.EnqueueGetAsyncResult(Array.Empty<BuildingWorkspace>());

        HttpResponseMessage response = await _client.GetAsync($"{ApiRoutes.Buildings}/1/floors?limit=1&offset=5&searchTerm=floor");

        response.EnsureSuccessStatusCode();

        IReadOnlyList<FloorWithRoomsModel>? floors = await response.Content.ReadFromJsonAsync<IReadOnlyList<FloorWithRoomsModel>>();
        floors.ShouldNotBeNull();
        floors.ShouldHaveSingleItem().Id.ShouldBe(5);

        FakePythagorasClient.RequestCapture floorRequest = _fakeClient.GetRequestsFor<Floor>().Single();
        floorRequest.QueryString.ShouldNotBeNull();
        floorRequest.QueryString.ShouldContain("generalSearch=floor");
        floorRequest.QueryString.ShouldContain("firstResult=5");
        floorRequest.QueryString.ShouldContain("maxResults=1");
        _fakeClient.LastEndpoint.ShouldBe("rest/v1/floor/5/workspace/info");
    }
}
