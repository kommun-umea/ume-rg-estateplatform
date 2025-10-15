using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging.Abstractions;
using Umea.se.EstateService.Logic.Exceptions;
using Umea.se.EstateService.Logic.Handlers;
using Umea.se.EstateService.Logic.Models;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enum;
using Umea.se.EstateService.Test.TestHelpers;

namespace Umea.se.EstateService.Test.Blueprint;

public class FloorBlueprintServiceTests
{
    [Fact]
    public async Task GetBlueprintAsync_ReturnsResultFromClient()
    {
        FakePythagorasClient client = new()
        {
            OnGetFloorBlueprintAsync = (_, _, _, _) =>
            {
                HttpResponseMessage response = new(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent([1, 2, 3])
                };
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = "source.pdf"
                };

                return Task.FromResult(response);
            }
        };

        FloorBlueprintService service = new(client, NullLogger<FloorBlueprintService>.Instance);

        FloorBlueprint result = await service.GetBlueprintAsync(42, BlueprintFormat.Pdf, includeWorkspaceTexts: false);

        result.FileName.ShouldBe("source.pdf");
        result.ContentType.ShouldBe("application/pdf");
        byte[] buffer = new byte[3];
        int read = await result.Content.ReadAsync(buffer);
        read.ShouldBe(3);
        buffer.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public async Task GetBlueprintAsync_WhenClientThrowsHttpRequestException_ThrowsUnavailable()
    {
        FakePythagorasClient client = new()
        {
            OnGetFloorBlueprintAsync = (_, _, _, _) => throw new HttpRequestException("fail")
        };

        FloorBlueprintService service = new(client, NullLogger<FloorBlueprintService>.Instance);

        await Should.ThrowAsync<FloorBlueprintUnavailableException>(() =>
            service.GetBlueprintAsync(5, BlueprintFormat.Svg, includeWorkspaceTexts: false));
    }

    [Fact]
    public async Task GetBlueprintAsync_AddsExtensionWhenMissing()
    {
        FakePythagorasClient client = new()
        {
            OnGetFloorBlueprintAsync = (_, _, _, _) =>
            {
                HttpResponseMessage response = new(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent([])
                };
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/svg+xml");
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = "floor"
                };

                return Task.FromResult(response);
            }
        };

        FloorBlueprintService service = new(client, NullLogger<FloorBlueprintService>.Instance);

        FloorBlueprint result = await service.GetBlueprintAsync(11, BlueprintFormat.Svg, includeWorkspaceTexts: false);

        result.FileName.ShouldBe("floor.svg");
    }

    [Fact]
    public async Task GetBlueprintAsync_WithInvalidFloorId_ThrowsValidationException()
    {
        FakePythagorasClient client = new();
        FloorBlueprintService service = new(client, NullLogger<FloorBlueprintService>.Instance);

        await Should.ThrowAsync<FloorBlueprintValidationException>(() =>
            service.GetBlueprintAsync(0, BlueprintFormat.Pdf, includeWorkspaceTexts: false));
    }
}
