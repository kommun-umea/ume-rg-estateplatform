using Microsoft.AspNetCore.Mvc;
using Umea.se.EstateService.API.Controllers;
using Umea.se.EstateService.API.Controllers.Requests;
using Umea.se.EstateService.Logic.Handlers;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Test.Pythagoras;

public class BuildingControllerTests
{
    [Fact]
    public async Task GetBuildingsAsync_ReturnsFirst50Buildings()
    {
        FakePythagorasClient client = new()
        {
            GetAsyncResult =
            [
                new() { Id = 1, Name = "One" },
                new() { Id = 2, Name = "Two" }
            ]
        };

        PythagorasHandler service = new(client);
        BuildingController controller = new(service);

        IReadOnlyList<BuildingInfoModel> buildings = await controller.GetBuildingsAsync(new BuildingListRequest(), CancellationToken.None);

        buildings.Select(b => b.Id).ShouldBe(new[] { 1, 2 });
        client.LastQueryString.ShouldBe("maxResults=50");
        client.LastEndpoint.ShouldBe("rest/v1/building/info");
    }

    [Fact]
    public async Task GetBuildingsAsync_WithSearchTerm_FiltersByNameContains()
    {
        FakePythagorasClient client = new()
        {
            GetAsyncResult =
            [
                new() { Id = 3, Name = "Alpha" }
            ]
        };

        PythagorasHandler service = new(client);
        BuildingController controller = new(service);

        IReadOnlyList<BuildingInfoModel> buildings = await controller.GetBuildingsAsync(
            new BuildingListRequest { SearchTerm = "alp" },
            CancellationToken.None);

        BuildingInfoModel building = buildings.ShouldHaveSingleItem();
        building.Name.ShouldBe("Alpha");

        client.LastQueryString.ShouldNotBeNull();
        client.LastQueryString.ShouldContain("generalSearch=alp");
        client.LastQueryString.ShouldContain("maxResults=50");
        client.LastEndpoint.ShouldBe("rest/v1/building/info");
    }

    [Fact]
    public async Task GetBuildingRoomsAsync_ReturnsMappedRooms()
    {
        FakePythagorasClient client = new()
        {
            GetBuildingWorkspacesResult =
            [
                new() { Id = 10, BuildingId = 1, BuildingName = "B" }
            ]
        };

        PythagorasHandler service = new(client);
        BuildingController controller = new(service);

        IReadOnlyList<BuildingRoomModel> result = await controller.GetBuildingRoomsAsync(1, new BuildingRoomsRequest(), CancellationToken.None);

        BuildingRoomModel room = result.ShouldHaveSingleItem();
        room.Id.ShouldBe(10);
        client.LastQueryString.ShouldBe("maxResults=50");
        client.LastEndpoint.ShouldBe("rest/v1/building/1/workspace/info");
    }

    [Fact]
    public async Task GetBuildingFloorsAsync_AppliesQueryParameters()
    {
        FakePythagorasClient client = new()
        {
            GetBuildingFloorsResult =
            [
                new() { Id = 5, Uid = Guid.NewGuid(), Name = "Floor 1" }
            ],
            GetBuildingWorkspacesResult = []
        };

        PythagorasHandler service = new(client);
        BuildingController controller = new(service);

        ActionResult<IReadOnlyList<FloorWithRoomsModel>> response = await controller.GetBuildingFloorsAsync(
            1,
            new BuildingFloorsRequest { Limit = 1, Offset = 5, SearchTerm = "floor" },
            CancellationToken.None);

        OkObjectResult ok = response.Result.ShouldBeOfType<OkObjectResult>();
        IReadOnlyList<FloorWithRoomsModel>? floors = ok.Value.ShouldBeAssignableTo<IReadOnlyList<FloorWithRoomsModel>>();
        floors.ShouldHaveSingleItem().Id.ShouldBe(5);

        client.LastFloorQueryString.ShouldNotBeNull();
        client.LastFloorQueryString.ShouldContain("generalSearch=floor");
        client.LastFloorQueryString.ShouldContain("firstResult=5");
        client.LastFloorQueryString.ShouldContain("maxResults=1");
        client.LastEndpoint.ShouldBe("rest/v1/floor/5/workspace/info");
    }

    private sealed class FakePythagorasClient : IPythagorasClient
    {
        public IReadOnlyList<BuildingInfo> GetAsyncResult { get; set; } = [];
        public IReadOnlyList<BuildingWorkspace> GetBuildingWorkspacesResult { get; set; } = [];
        public IReadOnlyList<Floor> GetBuildingFloorsResult { get; set; } = [];
        public string? LastQueryString { get; private set; }
        public string? LastFloorQueryString { get; private set; }
        public string? LastEndpoint { get; private set; }

        public Task<IReadOnlyList<TDto>> GetAsync<TDto>(string endpoint, PythagorasQuery<TDto>? query, CancellationToken cancellationToken) where TDto : class, IPythagorasDto
        {
            LastEndpoint = endpoint;
            LastQueryString = query?.BuildAsQueryString();

            if (typeof(TDto) == typeof(BuildingInfo))
            {
                return Task.FromResult((IReadOnlyList<TDto>)(object)GetAsyncResult);
            }

            if (typeof(TDto) == typeof(BuildingWorkspace))
            {
                return Task.FromResult((IReadOnlyList<TDto>)(object)GetBuildingWorkspacesResult);
            }

            if (typeof(TDto) == typeof(Floor))
            {
                LastFloorQueryString = LastQueryString;
                return Task.FromResult((IReadOnlyList<TDto>)(object)GetBuildingFloorsResult);
            }

            throw new NotSupportedException("Test fake only supports Building, Floor, and BuildingWorkspace DTOs.");
        }

        public IAsyncEnumerable<TDto> GetPaginatedAsync<TDto>(string endpoint, PythagorasQuery<TDto>? query, int pageSize, CancellationToken cancellationToken) where TDto : class, IPythagorasDto
            => throw new NotSupportedException();
    }
}
