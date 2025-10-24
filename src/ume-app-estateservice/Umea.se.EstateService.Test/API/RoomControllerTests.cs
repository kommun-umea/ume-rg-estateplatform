using Microsoft.AspNetCore.Mvc;
using Umea.se.EstateService.API.Controllers;
using Umea.se.EstateService.API.Controllers.Requests;
using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Test.API;

public class RoomControllerTests
{
    [Fact]
    public async Task GetRoomAsync_WhenFound_ReturnsRoom()
    {
        RoomModel room = new() { Id = 9 };

        StubPythagorasHandler handler = new()
        {
            OnGetRoomsAsync = (_, _) => Task.FromResult<IReadOnlyList<RoomModel>>([room])
        };

        RoomController controller = new(handler);

        ActionResult<RoomDetailsModel> action = await controller.GetRoomAsync(
            9,
            new RoomDetailsRequest(),
            CancellationToken.None);

        RoomDetailsModel result = action.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeOfType<RoomDetailsModel>();
        result.Room.ShouldBe(room);
        result.Building.ShouldBeNull();
    }

    [Fact]
    public async Task GetRoomAsync_WhenMissing_Returns404()
    {
        StubPythagorasHandler handler = new()
        {
            OnGetRoomsAsync = (_, _) => Task.FromResult<IReadOnlyList<RoomModel>>([])
        };

        RoomController controller = new(handler);

        ActionResult<RoomDetailsModel> action = await controller.GetRoomAsync(
            100,
            new RoomDetailsRequest(),
            CancellationToken.None);

        action.Result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetRoomAsync_WithIncludeBuilding_LoadsBuilding()
    {
        RoomModel room = new() { Id = 9, BuildingId = 44 };
        BuildingInfoModel building = new() { Id = 44 };

        PythagorasQuery<Workspace>? capturedRoomQuery = null;
        PythagorasQuery<BuildingInfo>? capturedBuildingQuery = null;

        StubPythagorasHandler handler = new()
        {
            OnGetRoomsAsync = (query, _) =>
            {
                capturedRoomQuery = query;
                return Task.FromResult<IReadOnlyList<RoomModel>>([room]);
            },
            OnGetBuildingsAsync = (query, _) =>
            {
                capturedBuildingQuery = query;
                return Task.FromResult<IReadOnlyList<BuildingInfoModel>>([building]);
            }
        };

        RoomController controller = new(handler);

        ActionResult<RoomDetailsModel> action = await controller.GetRoomAsync(
            9,
            new RoomDetailsRequest { IncludeBuilding = true },
            CancellationToken.None);

        RoomDetailsModel result = action.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeOfType<RoomDetailsModel>();
        result.Building.ShouldBe(building);

        capturedRoomQuery.ShouldNotBeNull();
        capturedBuildingQuery.ShouldNotBeNull();

        string roomQueryString = capturedRoomQuery!.BuildAsQueryString();
        roomQueryString.ShouldContain("pN%5B%5D=EQ%3Aid");
        roomQueryString.ShouldContain("pV%5B%5D=9");

        string buildingQueryString = capturedBuildingQuery!.BuildAsQueryString();
        buildingQueryString.ShouldContain("pN%5B%5D=EQ%3Aid");
        buildingQueryString.ShouldContain("pV%5B%5D=44");
    }

    private sealed class StubPythagorasHandler : IPythagorasHandler
    {
        public Func<PythagorasQuery<Workspace>?, CancellationToken, Task<IReadOnlyList<RoomModel>>>? OnGetRoomsAsync { get; set; }
        public Func<PythagorasQuery<BuildingInfo>?, CancellationToken, Task<IReadOnlyList<BuildingInfoModel>>>? OnGetBuildingsAsync { get; set; }

        public Task<IReadOnlyList<RoomModel>> GetRoomsAsync(PythagorasQuery<Workspace>? query = null, CancellationToken cancellationToken = default)
        {
            if (OnGetRoomsAsync is null)
            {
                throw new InvalidOperationException("OnGetRoomsAsync must be set.");
            }

            return OnGetRoomsAsync(query, cancellationToken);
        }

        public Task<IReadOnlyList<BuildingInfoModel>> GetBuildingsAsync(PythagorasQuery<BuildingInfo>? query = null, CancellationToken cancellationToken = default)
        {
            if (OnGetBuildingsAsync is null)
            {
                throw new InvalidOperationException("OnGetBuildingsAsync must be set.");
            }

            return OnGetBuildingsAsync(query, cancellationToken);
        }

        public Task<IReadOnlyList<EstateModel>> GetEstatesAsync(PythagorasQuery<NavigationFolder>? query = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IReadOnlyList<BuildingInfoModel>> GetBuildingInfoAsync(PythagorasQuery<BuildingInfo>? query = null, int? navigationFolderId = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IReadOnlyList<BuildingRoomModel>> GetBuildingWorkspacesAsync(int buildingId, PythagorasQuery<BuildingWorkspace>? query = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IReadOnlyList<FloorInfoModel>> GetBuildingFloorsAsync(int buildingId, PythagorasQuery<Floor>? floorQuery = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IReadOnlyList<FloorInfoModel>> GetBuildingFloorsWithRoomsAsync(int buildingId, PythagorasQuery<Floor>? floorQuery = null, PythagorasQuery<BuildingWorkspace>? workspaceQuery = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IReadOnlyList<BuildingAscendantModel>> GetBuildingAscendantsAsync(int buildingId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<BuildingAscendantModel>>([]);

        public Task<IReadOnlyList<FloorInfoModel>> GetFloorsAsync(PythagorasQuery<Floor>? query = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }
}
