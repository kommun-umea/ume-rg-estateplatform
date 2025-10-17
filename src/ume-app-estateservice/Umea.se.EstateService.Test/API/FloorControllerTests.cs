using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Umea.se.EstateService.API.Controllers;
using Umea.se.EstateService.API.Controllers.Requests;
using Umea.se.EstateService.Logic.Exceptions;
using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.Logic.Models;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enum;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Test.API;

public class FloorControllerTests
{
    [Fact]
    public async Task GetFloorAsync_WhenFound_ReturnsFloor()
    {
        FloorInfoModel floor = new() { Id = 1655, Name = "Test" };
        PythagorasQuery<Floor>? capturedQuery = null;

        StubPythagorasHandler handler = new()
        {
            OnGetFloorsAsync = (query, _) =>
            {
                capturedQuery = query;
                return Task.FromResult<IReadOnlyList<FloorInfoModel>>(new[] { floor });
            }
        };

        FloorController controller = new(new StubFloorBlueprintService(), handler, NullLogger<FloorController>.Instance);

        ActionResult<FloorInfoModel> result = await controller.GetFloorAsync(1655, CancellationToken.None);

        FloorInfoModel model = result.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeOfType<FloorInfoModel>();
        model.ShouldBe(floor);

        capturedQuery.ShouldNotBeNull();
        string decodedQuery = Uri.UnescapeDataString(capturedQuery!.BuildAsQueryString());
        decodedQuery.ShouldContain("floorIds[]=1655");
    }

    [Fact]
    public async Task GetFloorAsync_WhenMissing_Returns404()
    {
        StubPythagorasHandler handler = new()
        {
            OnGetFloorsAsync = (_, _) => Task.FromResult<IReadOnlyList<FloorInfoModel>>(Array.Empty<FloorInfoModel>())
        };

        FloorController controller = new(new StubFloorBlueprintService(), handler, NullLogger<FloorController>.Instance);

        ActionResult<FloorInfoModel> result = await controller.GetFloorAsync(999, CancellationToken.None);

        result.Result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetFloorBlueprintAsync_ReturnsFileWhenServiceSucceeds()
    {
        StubFloorBlueprintService service = new()
        {
            OnGetBlueprintAsync = (_, _, _, _) =>
            {
                MemoryStream stream = new([4, 5, 6]);
                FloorBlueprint blueprint = new(stream, "application/pdf", "floor.pdf");
                return Task.FromResult(blueprint);
            }
        };

        FloorController controller = new(service, new StubPythagorasHandler(), NullLogger<FloorController>.Instance);

        IActionResult result = await controller.GetFloorBlueprintAsync(
            10,
            new FloorBlueprintRequest { Format = BlueprintFormat.Pdf },
            CancellationToken.None);

        FileStreamResult fileResult = result.ShouldBeOfType<FileStreamResult>();
        fileResult.ContentType.ShouldBe("application/pdf");
        fileResult.FileDownloadName.ShouldBe("floor.pdf");
    }

    [Fact]
    public async Task GetFloorBlueprintAsync_WithInvalidFormat_Returns400()
    {
        FloorController controller = new(new StubFloorBlueprintService(), new StubPythagorasHandler(), NullLogger<FloorController>.Instance);
        controller.ModelState.Clear();
        controller.ModelState.AddModelError(nameof(FloorBlueprintRequest.Format), "Invalid format");

        IActionResult result = await controller.GetFloorBlueprintAsync(
            5,
            new FloorBlueprintRequest(),
            CancellationToken.None);

        BadRequestObjectResult problem = result.ShouldBeOfType<BadRequestObjectResult>();
        ValidationProblemDetails details = problem.Value.ShouldBeOfType<ValidationProblemDetails>();
        details.Status.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task GetFloorBlueprintAsync_WhenServiceUnavailable_Returns502()
    {
        StubFloorBlueprintService service = new()
        {
            OnGetBlueprintAsync = (_, _, _, _) => throw new FloorBlueprintUnavailableException("fail", new HttpRequestException())
        };

        FloorController controller = new(service, new StubPythagorasHandler(), NullLogger<FloorController>.Instance);

        IActionResult result = await controller.GetFloorBlueprintAsync(
            12,
            new FloorBlueprintRequest { Format = BlueprintFormat.Pdf },
            CancellationToken.None);

        ObjectResult objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(StatusCodes.Status502BadGateway);
    }

    private sealed class StubFloorBlueprintService : IFloorBlueprintService
    {
        public Func<int, BlueprintFormat, bool, CancellationToken, Task<FloorBlueprint>>? OnGetBlueprintAsync { get; set; }

        public Task<FloorBlueprint> GetBlueprintAsync(int floorId, BlueprintFormat format, bool includeWorkspaceTexts, CancellationToken cancellationToken = default)
        {
            if (OnGetBlueprintAsync is null)
            {
                throw new InvalidOperationException("OnGetBlueprintAsync must be set for this test.");
            }

            return OnGetBlueprintAsync(floorId, format, includeWorkspaceTexts, cancellationToken);
        }
    }

    private sealed class StubPythagorasHandler : IPythagorasHandler
    {
        public Func<PythagorasQuery<Floor>?, CancellationToken, Task<IReadOnlyList<FloorInfoModel>>>? OnGetFloorsAsync { get; set; }

        public Task<IReadOnlyList<FloorInfoModel>> GetFloorsAsync(PythagorasQuery<Floor>? query = null, CancellationToken cancellationToken = default)
        {
            if (OnGetFloorsAsync is null)
            {
                return Task.FromResult<IReadOnlyList<FloorInfoModel>>(Array.Empty<FloorInfoModel>());
            }

            return OnGetFloorsAsync(query, cancellationToken);
        }

        public Task<IReadOnlyList<EstateModel>> GetEstatesAsync(PythagorasQuery<NavigationFolder>? query = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IReadOnlyList<BuildingInfoModel>> GetBuildingsAsync(PythagorasQuery<BuildingInfo>? query = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IReadOnlyList<BuildingInfoModel>> GetBuildingInfoAsync(PythagorasQuery<BuildingInfo>? query = null, int? navigationFolderId = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IReadOnlyList<BuildingRoomModel>> GetBuildingWorkspacesAsync(int buildingId, PythagorasQuery<BuildingWorkspace>? query = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IReadOnlyList<FloorWithRoomsModel>> GetBuildingFloorsWithRoomsAsync(int buildingId, PythagorasQuery<Floor>? floorQuery = null, PythagorasQuery<BuildingWorkspace>? workspaceQuery = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IReadOnlyList<RoomModel>> GetRoomsAsync(PythagorasQuery<Workspace>? query = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }
}
