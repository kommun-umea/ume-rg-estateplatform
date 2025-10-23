using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Umea.se.EstateService.API.Controllers;
using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Test.API;

public class BuildingControllerTests
{
    [Fact]
    public async Task GetBuildingAsync_WhenFound_ReturnsBuildingWithAscendants()
    {
        BuildingInfoModel building = new() { Id = 42, Name = "Test" };
        BuildingAscendantModel ascendant = new() { Id = 7, Name = "Estate", Type = BuildingAscendantType.Estate };

        StubPythagorasHandler handler = new()
        {
            OnGetBuildingsAsync = (_, _) => Task.FromResult<IReadOnlyList<BuildingInfoModel>>([building]),
            OnGetBuildingAscendantsAsync = (_, _) => Task.FromResult<IReadOnlyList<BuildingAscendantModel>>([ascendant])
        };

        BuildingController controller = new(handler, NullLogger<BuildingController>.Instance);

        ActionResult<BuildingInfoModel> result = await controller.GetBuildingAsync(
            42,
            CancellationToken.None);

        BuildingInfoModel response = result.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeOfType<BuildingInfoModel>();
        response.Id.ShouldBe(42);
        response.Name.ShouldBe("Test");
        response.Ascendants.ShouldBe([ascendant]);
    }

    [Fact]
    public async Task GetBuildingAsync_WhenMissing_Returns404()
    {
        StubPythagorasHandler handler = new()
        {
            OnGetBuildingsAsync = (_, _) => Task.FromResult<IReadOnlyList<BuildingInfoModel>>([])
        };

        BuildingController controller = new(handler, NullLogger<BuildingController>.Instance);

        ActionResult<BuildingInfoModel> result = await controller.GetBuildingAsync(
            100,
            CancellationToken.None);

        result.Result.ShouldBeOfType<NotFoundResult>();
    }

    private sealed class StubPythagorasHandler : IPythagorasHandler
    {
        public Func<PythagorasQuery<BuildingInfo>?, CancellationToken, Task<IReadOnlyList<BuildingInfoModel>>>? OnGetBuildingsAsync { get; set; }
        public Func<int, CancellationToken, Task<IReadOnlyList<BuildingAscendantModel>>>? OnGetBuildingAscendantsAsync { get; set; }

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

        public Task<IReadOnlyList<BuildingAscendantModel>> GetBuildingAscendantsAsync(int buildingId, CancellationToken cancellationToken = default)
        {
            if (OnGetBuildingAscendantsAsync is null)
            {
                return Task.FromResult<IReadOnlyList<BuildingAscendantModel>>([]);
            }

            return OnGetBuildingAscendantsAsync(buildingId, cancellationToken);
        }

        public Task<IReadOnlyList<BuildingRoomModel>> GetBuildingWorkspacesAsync(int buildingId, PythagorasQuery<BuildingWorkspace>? query = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IReadOnlyList<FloorInfoModel>> GetBuildingFloorsAsync(int buildingId, PythagorasQuery<Floor>? floorQuery = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IReadOnlyList<FloorInfoModel>> GetBuildingFloorsWithRoomsAsync(int buildingId, PythagorasQuery<Floor>? floorQuery = null, PythagorasQuery<BuildingWorkspace>? workspaceQuery = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IReadOnlyList<RoomModel>> GetRoomsAsync(PythagorasQuery<Workspace>? query = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IReadOnlyList<FloorInfoModel>> GetFloorsAsync(PythagorasQuery<Floor>? query = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }
}
