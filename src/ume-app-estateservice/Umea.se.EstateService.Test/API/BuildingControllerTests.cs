using Microsoft.AspNetCore.Mvc;
using Umea.se.EstateService.API.Controllers;
using Umea.se.EstateService.API.Controllers.Requests;
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
        BuildingInfoModel building = new() { Id = 42, NavigationInfo = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase) };

        StubPythagorasHandler handler = new()
        {
            OnGetBuildingsAsync = (_, _) => Task.FromResult<IReadOnlyList<BuildingInfoModel>>(new[] { building })
        };

        BuildingController controller = new(handler);

        ActionResult<BuildingDetailsModel> result = await controller.GetBuildingAsync(
            42,
            new BuildingDetailsRequest(),
            CancellationToken.None);

        BuildingDetailsModel response = result.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeOfType<BuildingDetailsModel>();
        response.Building.ShouldBe(building);
        response.Estate.ShouldBeNull();
    }

    [Fact]
    public async Task GetBuildingAsync_WhenMissing_Returns404()
    {
        StubPythagorasHandler handler = new()
        {
            OnGetBuildingsAsync = (_, _) => Task.FromResult<IReadOnlyList<BuildingInfoModel>>(Array.Empty<BuildingInfoModel>())
        };

        BuildingController controller = new(handler);

        ActionResult<BuildingDetailsModel> result = await controller.GetBuildingAsync(
            100,
            new BuildingDetailsRequest(),
            CancellationToken.None);

        result.Result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetBuildingAsync_WithIncludeEstate_LoadsEstate()
    {
        Dictionary<string, string?> navigationInfo = new(StringComparer.OrdinalIgnoreCase)
        {
            ["navigationFolderId"] = "7"
        };

        BuildingInfoModel building = new() { Id = 7, NavigationInfo = navigationInfo };
        EstateModel estate = new() { Id = 7, Name = "Estate" };

        PythagorasQuery<BuildingInfo>? capturedBuildingQuery = null;
        PythagorasQuery<NavigationFolder>? capturedEstateQuery = null;

        StubPythagorasHandler handler = new()
        {
            OnGetBuildingsAsync = (query, _) =>
            {
                capturedBuildingQuery = query;
                return Task.FromResult<IReadOnlyList<BuildingInfoModel>>(new[] { building });
            },
            OnGetEstatesAsync = (query, _) =>
            {
                capturedEstateQuery = query;
                return Task.FromResult<IReadOnlyList<EstateModel>>(new[] { estate });
            }
        };

        BuildingController controller = new(handler);

        ActionResult<BuildingDetailsModel> response = await controller.GetBuildingAsync(
            7,
            new BuildingDetailsRequest { IncludeEstate = true },
            CancellationToken.None);

        BuildingDetailsModel result = response.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeOfType<BuildingDetailsModel>();
        result.Estate.ShouldBe(estate);

        string buildingQueryString = capturedBuildingQuery!.BuildAsQueryString();
        buildingQueryString.ShouldContain("pN%5B%5D=EQ%3Aid");
        buildingQueryString.ShouldContain("pV%5B%5D=7");

        string estateQueryString = capturedEstateQuery!.BuildAsQueryString();
        estateQueryString.ShouldContain("pN%5B%5D=EQ%3Aid");
        estateQueryString.ShouldContain("pV%5B%5D=7");
    }

    private sealed class StubPythagorasHandler : IPythagorasHandler
    {
        public Func<PythagorasQuery<BuildingInfo>?, CancellationToken, Task<IReadOnlyList<BuildingInfoModel>>>? OnGetBuildingsAsync { get; set; }
        public Func<PythagorasQuery<NavigationFolder>?, CancellationToken, Task<IReadOnlyList<EstateModel>>>? OnGetEstatesAsync { get; set; }

        public Task<IReadOnlyList<BuildingInfoModel>> GetBuildingsAsync(PythagorasQuery<BuildingInfo>? query = null, CancellationToken cancellationToken = default)
        {
            if (OnGetBuildingsAsync is null)
            {
                throw new InvalidOperationException("OnGetBuildingsAsync must be set.");
            }

            return OnGetBuildingsAsync(query, cancellationToken);
        }

        public Task<IReadOnlyList<EstateModel>> GetEstatesAsync(PythagorasQuery<NavigationFolder>? query = null, CancellationToken cancellationToken = default)
        {
            if (OnGetEstatesAsync is null)
            {
                throw new InvalidOperationException("OnGetEstatesAsync must be set.");
            }

            return OnGetEstatesAsync(query, cancellationToken);
        }

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
