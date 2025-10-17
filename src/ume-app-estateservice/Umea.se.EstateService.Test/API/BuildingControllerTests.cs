using Microsoft.AspNetCore.Mvc;
using Umea.se.EstateService.API.Controllers;
using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Test.API;

public class BuildingControllerTests
{
    [Fact]
    public async Task GetBuildingAsync_WhenFound_ReturnsBuilding()
    {
        BuildingInfoModel building = new() { Id = 42 };

        StubPythagorasHandler handler = new()
        {
            OnGetBuildingsAsync = (_, _) => Task.FromResult<IReadOnlyList<BuildingInfoModel>>(new[] { building })
        };

        BuildingController controller = new(handler);

        ActionResult<BuildingInfoModel> result = await controller.GetBuildingAsync(
            42,
            CancellationToken.None);

        BuildingInfoModel response = result.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeOfType<BuildingInfoModel>();
        response.ShouldBe(building);
    }

    [Fact]
    public async Task GetBuildingAsync_WhenMissing_Returns404()
    {
        StubPythagorasHandler handler = new()
        {
            OnGetBuildingsAsync = (_, _) => Task.FromResult<IReadOnlyList<BuildingInfoModel>>(Array.Empty<BuildingInfoModel>())
        };

        BuildingController controller = new(handler);

        ActionResult<BuildingInfoModel> result = await controller.GetBuildingAsync(
            100,
            CancellationToken.None);

        result.Result.ShouldBeOfType<NotFoundResult>();
    }

    private sealed class StubPythagorasHandler : IPythagorasHandler
    {
        public Func<PythagorasQuery<BuildingInfo>?, CancellationToken, Task<IReadOnlyList<BuildingInfoModel>>>? OnGetBuildingsAsync { get; set; }

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

        public Task<IReadOnlyList<FloorWithRoomsModel>> GetBuildingFloorsWithRoomsAsync(int buildingId, PythagorasQuery<Floor>? floorQuery = null, PythagorasQuery<BuildingWorkspace>? workspaceQuery = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IReadOnlyList<RoomModel>> GetRoomsAsync(PythagorasQuery<Workspace>? query = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IReadOnlyList<FloorInfoModel>> GetFloorsAsync(PythagorasQuery<Floor>? query = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }
}
