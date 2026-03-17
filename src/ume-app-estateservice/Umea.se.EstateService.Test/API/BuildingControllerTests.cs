using System.Net;
using System.Net.Http.Json;
using Umea.se.EstateService.API;
using Umea.se.EstateService.ServiceAccess;
using Umea.se.EstateService.Shared.Data.Entities;
using Umea.se.EstateService.Shared.Models;
using Umea.se.EstateService.Test.TestHelpers;
using Umea.se.TestToolkit.TestInfrastructure;

namespace Umea.se.EstateService.Test.API;

[Collection("DataStoreTests")]
public class BuildingControllerTests : ControllerTestCloud<TestApiFactory, Program, HttpClientNames>
{
    private readonly HttpClient _client;
    private readonly FakePythagorasClient _fakeClient;
    private readonly StubBuildingImageService _stubImageService;

    public BuildingControllerTests()
    {
        _client = Client;
        _client.DefaultRequestHeaders.Add("X-Api-Key", TestApiFactory.ApiKey);
        _fakeClient = WebAppFactory.FakeClient;
        _stubImageService = WebAppFactory.StubImageService;

        MockManager.SetupUser(user => user.WithEmail("test@example.com").WithActualAuthorization());

        // Ensure a clean datastore snapshot before each test in this class.
        DataStoreSeeder.Clear(WebAppFactory.GetDataStore());
    }

    [Fact]
    public async Task GetBuildingsAsync_ReturnsFirst50Buildings()
    {
        DataStoreSeeder.Seed(
            WebAppFactory.GetDataStore(),
            buildings:
            [
                new BuildingEntity { Id = 1, Name = "One", PopularName = "One" },
                new BuildingEntity { Id = 2, Name = "Two", PopularName = "Two" }
            ]);

        HttpResponseMessage response = await _client.GetAsync(ApiRoutes.Buildings);
        response.EnsureSuccessStatusCode();

        IReadOnlyList<BuildingInfoModel>? buildings = await response.Content.ReadFromJsonAsync<IReadOnlyList<BuildingInfoModel>>();
        buildings.ShouldNotBeNull();

        buildings.Select(b => b.Id).ShouldBe([1, 2]);
    }

    [Fact]
    public async Task GetBuildingsAsync_WithSearchTerm_FiltersByNameContains()
    {
        DataStoreSeeder.Seed(
            WebAppFactory.GetDataStore(),
            buildings:
            [
                new BuildingEntity { Id = 3, Name = "Alpha", PopularName = "Alpha" },
                new BuildingEntity { Id = 4, Name = "Beta", PopularName = "Beta" }
            ]);

        HttpResponseMessage response = await _client.GetAsync($"{ApiRoutes.Buildings}?searchTerm=alp");
        response.EnsureSuccessStatusCode();

        IReadOnlyList<BuildingInfoModel>? buildings = await response.Content.ReadFromJsonAsync<IReadOnlyList<BuildingInfoModel>>();
        buildings.ShouldNotBeNull();

        BuildingInfoModel building = buildings.ShouldHaveSingleItem();
        building.Name.ShouldBe("Alpha");
    }

    [Fact]
    public async Task GetBuildingRoomsAsync_ReturnsMappedRooms()
    {
        DataStoreSeeder.Seed(
            WebAppFactory.GetDataStore(),
            buildings:
            [
                new BuildingEntity
                {
                    Id = 1,
                    Name = "B",
                    PopularName = "B"
                }
            ],
            rooms:
            [
                new RoomEntity
                {
                    Id = 10,
                    BuildingId = 1,
                    Name = "Room 10",
                    PopularName = "Room 10",
                    GrossArea = 10,
                    NetArea = 8,
                    Capacity = 1
                }
            ]);

        HttpResponseMessage response = await _client.GetAsync($"{ApiRoutes.Buildings}/1/rooms");
        response.EnsureSuccessStatusCode();

        IReadOnlyList<RoomModel>? result = await response.Content.ReadFromJsonAsync<IReadOnlyList<RoomModel>>();
        result.ShouldNotBeNull();

        RoomModel room = result.ShouldHaveSingleItem();
        room.Id.ShouldBe(10);
    }

    [Fact]
    public async Task GetBuildingFloorsAsync_AppliesQueryParameters()
    {
        DataStoreSeeder.Clear(WebAppFactory.GetDataStore());
        DataStoreSeeder.Seed(
            WebAppFactory.GetDataStore(),
            buildings:
            [
                new BuildingEntity
                {
                    Id = 1,
                    Name = "Building 1",
                    PopularName = "Building 1"
                }
            ],
            floors: Enumerable.Range(1, 10)
                .Select(i => new FloorEntity
                {
                    Id = i,
                    Uid = Guid.NewGuid(),
                    BuildingId = 1,
                    Name = $"Floor {i}",
                    PopularName = $"Floor {i}"
                }));

        HttpResponseMessage response = await _client.GetAsync($"{ApiRoutes.Buildings}/1/floors?limit=1&offset=5&searchTerm=floor");

        response.EnsureSuccessStatusCode();

        IReadOnlyList<FloorInfoModel>? floors = await response.Content.ReadFromJsonAsync<IReadOnlyList<FloorInfoModel>>();
        floors.ShouldNotBeNull();
        FloorInfoModel floor = floors.ShouldHaveSingleItem();
        // With searchTerm applied first, offset=5, limit=1 should return the 6th matching floor.
        floor.Id.ShouldBe(6);
        floor.Rooms.ShouldBeNull();
    }

    [Fact]
    public async Task GetBuildingFloorsAsync_IncludeRoomsFalse_DoesNotFetchRooms()
    {
        DataStoreSeeder.Seed(
            WebAppFactory.GetDataStore(),
            buildings:
            [
                new BuildingEntity
                {
                    Id = 1,
                    Name = "Building 1",
                    PopularName = "Building 1"
                }
            ],
            floors:
            [
                new FloorEntity
                {
                    Id = 8,
                    Uid = Guid.NewGuid(),
                    BuildingId = 1,
                    Name = "Floor 2",
                    PopularName = "Floor 2"
                }
            ]);

        HttpResponseMessage response = await _client.GetAsync($"{ApiRoutes.Buildings}/1/floors?includeRooms=false");

        response.EnsureSuccessStatusCode();

        IReadOnlyList<FloorInfoModel>? floors = await response.Content.ReadFromJsonAsync<IReadOnlyList<FloorInfoModel>>();
        floors.ShouldNotBeNull();

        FloorInfoModel floor = floors.ShouldHaveSingleItem();
        floor.Id.ShouldBe(8);
        floor.Rooms.ShouldBeNull();
    }

    [Fact]
    public async Task GetBuildingAsync_ReturnsAscendantFields()
    {
        DataStoreSeeder.Seed(
            WebAppFactory.GetDataStore(),
            buildings:
            [
                new BuildingEntity
                {
                    Id = 1,
                    Name = "Alpha",
                    PopularName = "Alpha",
                    EstateId = 10
                }
            ]);

        HttpResponseMessage response = await _client.GetAsync($"{ApiRoutes.Buildings}/1");
        response.EnsureSuccessStatusCode();

        BuildingInfoModel? building = await response.Content.ReadFromJsonAsync<BuildingInfoModel>();
        building.ShouldNotBeNull();
        building.Id.ShouldBe(1);
        building.Name.ShouldBe("Alpha");
        // Ascendants are null because DataStoreSeeder.Seed does not seed buildingAscendants for this test.
        building.Estate.ShouldBeNull();
        building.Region.ShouldBeNull();
        building.Organization.ShouldBeNull();
    }

    [Fact]
    public async Task GetBuildingImageAsync_ReturnsImage()
    {
        _stubImageService.Reset();

        byte[] expected = [1, 2, 3, 4];
        _stubImageService.ImageResult = Umea.se.Toolkit.Images.ImageResult.WebP(expected);

        HttpResponseMessage result = await _client.GetAsync($"{ApiRoutes.Buildings}/1/image");

        result.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Content.Headers.ContentType.ShouldNotBeNull();
        result.Content.Headers.ContentType!.MediaType.ShouldBe("image/webp");

        byte[] payload = await result.Content.ReadAsByteArrayAsync();
        payload.ShouldBe(expected);

        _stubImageService.ImageRequestedForBuildingIds.ShouldBe([1]);
    }

    [Fact]
    public async Task GetBuildingImageAsync_NoImages_ReturnsNotFound()
    {
        _stubImageService.Reset();
        _stubImageService.ImageResult = null;

        HttpResponseMessage result = await _client.GetAsync($"{ApiRoutes.Buildings}/1/image");

        result.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
