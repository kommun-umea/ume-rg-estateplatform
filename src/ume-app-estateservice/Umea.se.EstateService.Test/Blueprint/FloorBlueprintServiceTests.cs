using System.Collections.Immutable;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Umea.se.EstateService.Logic.Data;
using Umea.se.EstateService.Logic.Handlers;
using Umea.se.EstateService.Logic.Handlers.Blueprint;
using Umea.se.EstateService.Logic.Models;
using Umea.se.EstateService.Shared.Exceptions;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enums;
using Umea.se.EstateService.Shared.Data;
using Umea.se.EstateService.Shared.Data.Entities;
using Umea.se.EstateService.Test.TestHelpers;
using Umea.se.Toolkit.Images;
using ZiggyCreatures.Caching.Fusion;

namespace Umea.se.EstateService.Test.Blueprint;

public class FloorBlueprintServiceTests
{
    private static ImageService CreateImageService()
    {
        ImageServiceOptions options = new() { CacheKeyPrefix = "test" };
        FusionCache cache = new(new FusionCacheOptions());
        return new ImageService(cache, options, NullLogger<ImageService>.Instance);
    }

    private static EstateDataQueryHandler CreateHandler(params RoomEntity[] rooms)
    {
        InMemoryDataStore dataStore = new();
        if (rooms.Length > 0)
        {
            dataStore.SetSnapshot(new DataSnapshot(
                estates: [],
                buildings: [],
                floors: [],
                rooms: [.. rooms],
                buildingAscendants: ImmutableDictionary<int, BuildingAscendantTriplet>.Empty,
                refreshUtc: DateTimeOffset.UtcNow));
        }

        return new EstateDataQueryHandler(dataStore);
    }

    private static async Task<string> ReadContentAsync(FloorBlueprint blueprint)
    {
        blueprint.Content.Position = 0;

        if (blueprint.IsGzipped)
        {
            await using GZipStream gzip = new(blueprint.Content, CompressionMode.Decompress, leaveOpen: true);
            using StreamReader reader = new(gzip, Encoding.UTF8);
            return await reader.ReadToEndAsync();
        }

        using StreamReader directReader = new(blueprint.Content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        return await directReader.ReadToEndAsync();
    }

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

        EstateDataQueryHandler handler = CreateHandler();
        FloorBlueprintHandler fbHandler = new(client, handler, CreateImageService(), NullLogger<FloorBlueprintHandler>.Instance);

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

        EstateDataQueryHandler handler = CreateHandler();
        FloorBlueprintHandler fbHandler = new(client, handler, CreateImageService(), NullLogger<FloorBlueprintHandler>.Instance);

        await Should.ThrowAsync<ExternalServiceUnavailableException>(() =>
            fbHandler.GetBlueprintAsync(5, BlueprintFormat.Svg, includeWorkspaceTexts: false));
    }

    [Fact]
    public async Task GetBlueprintAsync_Svg_GeneratesFileName()
    {
        byte[] svgBytes = Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"></svg>");
        FakePythagorasClient client = new()
        {
            OnGetFloorBlueprintAsync = (_, _, _, _) =>
            {
                HttpResponseMessage response = new(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(svgBytes)
                };
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/svg+xml");

                return Task.FromResult(response);
            }
        };

        EstateDataQueryHandler handler = CreateHandler();
        FloorBlueprintHandler fbHandler = new(client, handler, CreateImageService(), NullLogger<FloorBlueprintHandler>.Instance);

        FloorBlueprint result = await fbHandler.GetBlueprintAsync(11, BlueprintFormat.Svg, includeWorkspaceTexts: false);

        result.FileName.ShouldBe("floor-11.svg");
        result.ContentType.ShouldBe("image/svg+xml");
        result.IsGzipped.ShouldBeTrue();
    }

    [Fact]
    public async Task GetBlueprintAsync_Pdf_AddsExtensionWhenMissing()
    {
        FakePythagorasClient client = new()
        {
            OnGetFloorBlueprintAsync = (_, _, _, _) =>
            {
                HttpResponseMessage response = new(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent([])
                };
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = "floor"
                };

                return Task.FromResult(response);
            }
        };

        EstateDataQueryHandler handler = CreateHandler();
        FloorBlueprintHandler fbHandler = new(client, handler, CreateImageService(), NullLogger<FloorBlueprintHandler>.Instance);

        FloorBlueprint result = await fbHandler.GetBlueprintAsync(11, BlueprintFormat.Pdf, includeWorkspaceTexts: false);

        result.FileName.ShouldBe("floor.pdf");
        result.ContentType.ShouldBe("application/pdf");
        result.IsGzipped.ShouldBeFalse();
    }

    [Fact]
    public async Task GetBlueprintAsync_WithInvalidFloorId_ThrowsValidationException()
    {
        FakePythagorasClient client = new();
        EstateDataQueryHandler handler = CreateHandler();
        FloorBlueprintHandler fbHandler = new(client, handler, CreateImageService(), NullLogger<FloorBlueprintHandler>.Instance);

        await Should.ThrowAsync<BusinessValidationException>(() =>
            fbHandler.GetBlueprintAsync(0, BlueprintFormat.Pdf, includeWorkspaceTexts: false));
    }

    [Fact]
    public async Task GetBlueprintAsync_WhenWorkspaceTextsRequested_PassesRoomNamesToClient()
    {
        IDictionary<int, IReadOnlyList<string>>? capturedTexts = null;

        FakePythagorasClient client = new()
        {
            OnGetFloorBlueprintAsync = (_, _, texts, _) =>
            {
                capturedTexts = texts;

                HttpResponseMessage response = new(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent([1])
                };
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

                return Task.FromResult(response);
            }
        };

        EstateDataQueryHandler handler = CreateHandler(
            new RoomEntity { Id = 5, Name = "Alpha", PopularName = "Popular Alpha", BuildingId = 1, FloorId = 99 },
            new RoomEntity { Id = 6, Name = "Beta", BuildingId = 1, FloorId = 99 });

        FloorBlueprintHandler fbHandler = new(client, handler, CreateImageService(), NullLogger<FloorBlueprintHandler>.Instance);

        FloorBlueprint result = await fbHandler.GetBlueprintAsync(99, BlueprintFormat.Pdf, includeWorkspaceTexts: true);
        result.ShouldNotBeNull();

        capturedTexts.ShouldNotBeNull();
        capturedTexts!.ShouldContainKey(5);
        capturedTexts.ShouldContainKey(6);
        capturedTexts[5].ShouldBe(["Popular Alpha"]);
        capturedTexts[6].ShouldBe(["Beta"]);
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

        EstateDataQueryHandler handler = CreateHandler();
        FloorBlueprintHandler fbHandler = new(client, handler, CreateImageService(), NullLogger<FloorBlueprintHandler>.Instance);

        FloorBlueprint result = await fbHandler.GetBlueprintAsync(5, BlueprintFormat.Svg, includeWorkspaceTexts: false);

        result.IsGzipped.ShouldBeTrue();
        string cleaned = await ReadContentAsync(result);

        cleaned.ShouldNotContain("svgPageBorder");
        cleaned.ShouldNotContain("svgSignature");
        cleaned.ShouldNotContain("svgStamp");
        cleaned.ShouldContain("viewBox=\"0 0 200 200\"");
        cleaned.ShouldContain("preserveAspectRatio=\"xMidYMid\"");
        cleaned.ShouldNotContain(" x=\"10\"");
        cleaned.ShouldNotContain(" y=\"20\"");
    }

    [Fact]
    public async Task GetBlueprintAsync_WhenSvg_NormalizesFontSizes()
    {
        string sourceSvg =
            """
            <svg viewBox="0 0 100 100" xmlns="http://www.w3.org/2000/svg">
              <text font-size="0.4">Room A</text>
              <text style="font-size:0.3">Room B</text>
              <text style="font-size:0.5px">Room C</text>
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

                return Task.FromResult(response);
            }
        };

        EstateDataQueryHandler handler = CreateHandler();
        FloorBlueprintHandler fbHandler = new(client, handler, CreateImageService(), NullLogger<FloorBlueprintHandler>.Instance);

        FloorBlueprint result = await fbHandler.GetBlueprintAsync(7, BlueprintFormat.Svg, includeWorkspaceTexts: false);

        result.IsGzipped.ShouldBeTrue();
        string cleaned = await ReadContentAsync(result);

        cleaned.ShouldContain("font-size=\"0.4px\"");
        cleaned.ShouldContain("font-size:0.3px");
        cleaned.ShouldContain("font-size:0.5px");
    }
}
