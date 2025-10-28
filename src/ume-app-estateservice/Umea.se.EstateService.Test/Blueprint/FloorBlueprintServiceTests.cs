using System.Collections.Generic;
using System.Threading;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Umea.se.EstateService.Logic.Exceptions;
using Umea.se.EstateService.Logic.Handlers;
using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.Logic.Models;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enum;
using Umea.se.EstateService.Shared.Models;
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

        StubPythagorasHandler handler = new();
        FloorBlueprintHandler fbHandler = new(client, handler, NullLogger<FloorBlueprintHandler>.Instance);

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

        StubPythagorasHandler handler = new();
        FloorBlueprintHandler fbHandler = new(client, handler, NullLogger<FloorBlueprintHandler>.Instance);

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

        StubPythagorasHandler handler = new();
        FloorBlueprintHandler fbHandler = new(client, handler, NullLogger<FloorBlueprintHandler>.Instance);

        FloorBlueprint result = await fbHandler.GetBlueprintAsync(11, BlueprintFormat.Svg, includeWorkspaceTexts: false);

        result.FileName.ShouldBe("floor.svg");
    }

    [Fact]
    public async Task GetBlueprintAsync_WithInvalidFloorId_ThrowsValidationException()
    {
        FakePythagorasClient client = new();
        StubPythagorasHandler handler = new();
        FloorBlueprintHandler fbHandler = new(client, handler, NullLogger<FloorBlueprintHandler>.Instance);

        await Should.ThrowAsync<FloorBlueprintValidationException>(() =>
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

        StubPythagorasHandler handler = new()
        {
            OnGetRoomsAsync = (query, _) =>
            {
                RoomModel[] rooms =
                [
                    new() { Id = 5, Name = "Alpha", PopularName = "Popular Alpha" },
                    new() { Id = 6, Name = "Beta" }
                ];

                return Task.FromResult<IReadOnlyList<RoomModel>>(rooms);
            }
        };

        FloorBlueprintHandler fbHandler = new(client, handler, NullLogger<FloorBlueprintHandler>.Instance);

        FloorBlueprint result = await fbHandler.GetBlueprintAsync(99, BlueprintFormat.Pdf, includeWorkspaceTexts: true);
        result.ShouldNotBeNull();

        capturedTexts.ShouldNotBeNull();
        capturedTexts!.ShouldContainKey(5);
        capturedTexts.ShouldContainKey(6);
        capturedTexts[5].ShouldBe(["Popular Alpha"]);
        capturedTexts[6].ShouldBe(["Beta"]);

        handler.CapturedRoomsQuery.ShouldNotBeNull();
        string decodedQuery = System.Uri.UnescapeDataString(handler.CapturedRoomsQuery!.BuildAsQueryString());
        decodedQuery.ShouldContain("floorId");
        decodedQuery.ShouldContain("99");
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

        StubPythagorasHandler handler = new();
        FloorBlueprintHandler fbHandler = new(client, handler, NullLogger<FloorBlueprintHandler>.Instance);

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

    private sealed class StubPythagorasHandler : IPythagorasHandler
    {
        public PythagorasQuery<Workspace>? CapturedRoomsQuery { get; private set; }

        public Func<PythagorasQuery<Workspace>?, CancellationToken, Task<IReadOnlyList<RoomModel>>>? OnGetRoomsAsync { get; set; }

        public Task<IReadOnlyList<BuildingAscendantModel>> GetBuildingAscendantsAsync(int buildingId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<BuildingInfoModel?> GetBuildingByIdAsync(int buildingId, BuildingIncludeOptions includeOptions = BuildingIncludeOptions.None, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<FloorInfoModel>> GetBuildingFloorsAsync(int buildingId, PythagorasQuery<Floor>? floorQuery = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<FloorInfoModel>> GetBuildingFloorsWithRoomsAsync(int buildingId, PythagorasQuery<Floor>? floorQuery = null, PythagorasQuery<Workspace>? workspaceQuery = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<BuildingInfoModel>> GetBuildingsAsync(PythagorasQuery<BuildingInfo>? query = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<BuildingInfoModel>> GetBuildingsWithPropertiesAsync(IReadOnlyCollection<int>? buildingIds = null, IReadOnlyCollection<int>? propertyIds = null, int? navigationId = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<BuildingRoomModel>> GetBuildingWorkspacesAsync(int buildingId, PythagorasQuery<Workspace>? query = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyDictionary<int, BuildingWorkspaceStatsModel>> GetBuildingWorkspaceStatsAsync(PythagorasQuery<Workspace>? query = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<EstateModel?> GetEstateByIdAsync(int estateId, bool includeBuildings = false, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<EstateModel>> GetEstatesWithBuildingsAsync(PythagorasQuery<NavigationFolder>? query = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<EstateModel>> GetEstatesWithPropertiesAsync(IReadOnlyCollection<int>? estateIds = null, IReadOnlyCollection<int>? propertyIds = null, int? navigationId = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<FloorInfoModel>> GetFloorsAsync(PythagorasQuery<Floor>? query = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<RoomModel>> GetRoomsAsync(PythagorasQuery<Workspace>? query = null, CancellationToken cancellationToken = default)
        {
            CapturedRoomsQuery = query;
            if (OnGetRoomsAsync is null)
            {
                return Task.FromResult<IReadOnlyList<RoomModel>>([]);
            }

            return OnGetRoomsAsync(query, cancellationToken);
        }
    }
}
