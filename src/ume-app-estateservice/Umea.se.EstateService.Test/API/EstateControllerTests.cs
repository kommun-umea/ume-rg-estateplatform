using Microsoft.AspNetCore.Mvc;
using Umea.se.EstateService.API.Controllers;
using Umea.se.EstateService.API.Controllers.Requests;
using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Test.API;

public class EstateControllerTests
{
    [Fact]
    public async Task GetEstateAsync_WhenFound_ReturnsEstate()
    {
        EstateModel estate = new() { Id = 10, Name = "Central" };
        StubPythagorasHandler handler = new()
        {
            OnGetEstatesAsync = (_, _) => Task.FromResult<IReadOnlyList<EstateModel>>([estate])
        };

        EstateController controller = new(handler);

        ActionResult<EstateModel> response = await controller.GetEstateAsync(
            10,
            new EstateDetailsRequest(),
            CancellationToken.None);

        OkObjectResult ok = response.Result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBe(estate);
    }

    [Fact]
    public async Task GetEstateAsync_WhenMissing_Returns404()
    {
        StubPythagorasHandler handler = new()
        {
            OnGetEstatesAsync = (_, _) => Task.FromResult<IReadOnlyList<EstateModel>>([])
        };

        EstateController controller = new(handler);

        ActionResult<EstateModel> response = await controller.GetEstateAsync(
            99,
            new EstateDetailsRequest(),
            CancellationToken.None);

        response.Result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetEstateAsync_WithIncludeBuildings_AddsQueryParameter()
    {
        PythagorasQuery<NavigationFolder>? capturedQuery = null;

        StubPythagorasHandler handler = new()
        {
            OnGetEstatesAsync = (query, _) =>
            {
                capturedQuery = query;
                return Task.FromResult<IReadOnlyList<EstateModel>>([new EstateModel { Id = 5 }]);
            }
        };

        EstateController controller = new(handler);

        ActionResult<EstateModel> action = await controller.GetEstateAsync(
            5,
            new EstateDetailsRequest { IncludeBuildings = true },
            CancellationToken.None);

        action.Result.ShouldBeOfType<OkObjectResult>();
        capturedQuery.ShouldNotBeNull();
        string queryString = capturedQuery!.BuildAsQueryString();
        queryString.ShouldContain("includeAscendantBuildings=true");
        queryString.ShouldContain("pN%5B%5D=EQ%3Aid");
        queryString.ShouldContain("pV%5B%5D=5");
    }

    private sealed class StubPythagorasHandler : IPythagorasHandler
    {
        public Func<PythagorasQuery<NavigationFolder>?, CancellationToken, Task<IReadOnlyList<EstateModel>>>? OnGetEstatesAsync { get; set; }

        public Task<IReadOnlyList<EstateModel>> GetEstatesAsync(PythagorasQuery<NavigationFolder>? query = null, CancellationToken cancellationToken = default)
        {
            if (OnGetEstatesAsync is null)
            {
                throw new InvalidOperationException("OnGetEstatesAsync must be assigned for this test.");
            }

            return OnGetEstatesAsync(query, cancellationToken);
        }

        public Task<IReadOnlyList<BuildingInfoModel>> GetBuildingsAsync(PythagorasQuery<BuildingInfo>? query = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IReadOnlyList<BuildingInfoModel>> GetBuildingInfoAsync(PythagorasQuery<BuildingInfo>? query = null, int? navigationFolderId = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

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
