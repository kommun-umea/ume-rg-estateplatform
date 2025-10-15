using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Umea.se.EstateService.API.Controllers;
using Umea.se.EstateService.API.Controllers.Requests;
using Umea.se.EstateService.Logic.Exceptions;
using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.Logic.Models;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enum;

namespace Umea.se.EstateService.Test.API;

public class FloorControllerTests
{
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

        FloorController controller = new(service, NullLogger<FloorController>.Instance);

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
        FloorController controller = new(new StubFloorBlueprintService(), NullLogger<FloorController>.Instance);
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

        FloorController controller = new(service, NullLogger<FloorController>.Instance);

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
}
