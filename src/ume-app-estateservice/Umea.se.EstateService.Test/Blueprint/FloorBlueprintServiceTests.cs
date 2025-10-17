using System.Net;
using System.Net.Http.Headers;
using System.Text;
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

        FloorBlueprintHandler fbHandler = new(client, NullLogger<FloorBlueprintHandler>.Instance);

        FloorBlueprint result = await fbHandler.GetBlueprintAsync(42, BlueprintFormat.Pdf, includeWorkspaceTexts: false);

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

        FloorBlueprintHandler fbHandler = new(client, NullLogger<FloorBlueprintHandler>.Instance);

        await Should.ThrowAsync<FloorBlueprintUnavailableException>(() =>
            fbHandler.GetBlueprintAsync(5, BlueprintFormat.Svg, includeWorkspaceTexts: false));
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

        FloorBlueprintHandler fbHandler = new(client, NullLogger<FloorBlueprintHandler>.Instance);

        FloorBlueprint result = await fbHandler.GetBlueprintAsync(11, BlueprintFormat.Svg, includeWorkspaceTexts: false);

        result.FileName.ShouldBe("floor.svg");
    }

    [Fact]
    public async Task GetBlueprintAsync_WithInvalidFloorId_ThrowsValidationException()
    {
        FakePythagorasClient client = new();
        FloorBlueprintHandler fbHandler = new(client, NullLogger<FloorBlueprintHandler>.Instance);

        await Should.ThrowAsync<FloorBlueprintValidationException>(() =>
            fbHandler.GetBlueprintAsync(0, BlueprintFormat.Pdf, includeWorkspaceTexts: false));
    }

    [Fact]
    public async Task GetBlueprintAsync_WhenSvg_CleansAndCropsDocument()
    {
        string sourceSvg =
            """
            <svg viewBox="0 0 100 100" xmlns="http://www.w3.org/2000/svg" x="10" y="20">
              <title>Blueprint</title>
              <rect id="svgPageBorder" width="100" height="100" fill="none" stroke="red" />
              <svg id="inner" viewBox="0 0 200 200" preserveAspectRatio="xMidYMid">
                <rect id="svgSignature" width="10" height="10" />
                <rect id="svgStamp" width="5" height="5" />
                <path id="room" d="M0 0 L10 0 L10 10 Z" />
              </svg>
            </svg>
            """;

        FakePythagorasClient client = new()
        {
            OnGetFloorBlueprintAsync = (_, _, _, _) =>
            {
                byte[] payload = Encoding.UTF8.GetBytes(sourceSvg);
                HttpResponseMessage response = new(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(payload)
                };
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/svg+xml");
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = "sample.svg"
                };

                return Task.FromResult(response);
            }
        };

        FloorBlueprintHandler fbHandler = new(client, NullLogger<FloorBlueprintHandler>.Instance);

        FloorBlueprint result = await fbHandler.GetBlueprintAsync(5, BlueprintFormat.Svg, includeWorkspaceTexts: false);

        result.Content.Position = 0;
        using StreamReader reader = new(result.Content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        string cleaned = await reader.ReadToEndAsync();

        cleaned.ShouldNotContain("svgPageBorder");
        cleaned.ShouldNotContain("svgSignature");
        cleaned.ShouldNotContain("svgStamp");
        cleaned.ShouldContain("viewBox=\"0 0 200 200\"");
        cleaned.ShouldContain("preserveAspectRatio=\"xMidYMid\"");
        cleaned.ShouldNotContain(" x=\"10\"");
        cleaned.ShouldNotContain(" y=\"20\"");
    }
}
