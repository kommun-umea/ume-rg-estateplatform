using System.Net;
using System.Net.Http.Headers;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Umea.se.EstateService.Logic.Handlers;
using Umea.se.EstateService.Logic.Models;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enum;
using Umea.se.EstateService.Shared.Models;
using Umea.se.Toolkit.Images;
using ZiggyCreatures.Caching.Fusion;

namespace Umea.se.EstateService.Logic.Handlers.Blueprint;

public sealed class FloorBlueprintHandler(IPythagorasClient pythagorasClient, IEstateDataQueryHandler pythagorasHandler, ImageService imageService, ILogger<FloorBlueprintHandler> logger) : IFloorBlueprintService
{
    private static readonly string[] _nodesToRemove =
    [
        "svgPageBorder",
        "svgSignature",
        "svgStamp"
    ];

    private readonly IPythagorasClient _pythagorasClient = pythagorasClient;
    private readonly IEstateDataQueryHandler _pythagorasHandler = pythagorasHandler;
    private readonly ImageService _imageService = imageService;
    private readonly ILogger<FloorBlueprintHandler> _logger = logger;

    public async Task<FloorBlueprint> GetBlueprintAsync(int floorId, BlueprintFormat format, bool includeWorkspaceTexts = true, CancellationToken cancellationToken = default)
    {
        if (floorId <= 0)
        {
            throw new FloorBlueprintValidationException("Floor id must be positive.");
        }

        IDictionary<int, IReadOnlyList<string>>? workspaceTexts = await GetWorkspaceTextsAsync(floorId, includeWorkspaceTexts, cancellationToken).ConfigureAwait(false);

        if (format == BlueprintFormat.Svg)
        {
            return await GetCachedSvgBlueprintAsync(floorId, workspaceTexts, cancellationToken).ConfigureAwait(false);
        }

        return await FetchBlueprintFromPythagorasAsync(floorId, format, workspaceTexts, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IDictionary<int, IReadOnlyList<string>>?> GetWorkspaceTextsAsync(int floorId, bool includeWorkspaceTexts, CancellationToken cancellationToken)
    {
        if (!includeWorkspaceTexts)
        {
            return null;
        }

        _logger.LogInformation("Workspace text enrichment requested for floor {FloorId}. Fetching room data.", floorId);

        IReadOnlyList<RoomModel> rooms = await _pythagorasHandler
            .GetRoomsAsync(roomIds: null, buildingId: null, floorId: floorId, queryArgs: null, cancellationToken)
            .ConfigureAwait(false);

        if (rooms.Count == 0)
        {
            _logger.LogInformation("Workspace text enrichment requested, but no rooms found for floor {FloorId}.", floorId);
            return null;
        }

        _logger.LogInformation("Found {Count} rooms to include in blueprint for floor {FloorId}.", rooms.Count, floorId);

        return rooms.ToDictionary(
            static room => room.Id,
            static room => (IReadOnlyList<string>)[ResolveWorkspaceText(room)]);
    }

    private async Task<FloorBlueprint> GetCachedSvgBlueprintAsync(int floorId, IDictionary<int, IReadOnlyList<string>>? workspaceTexts, CancellationToken cancellationToken)
    {
        string cacheKey = $"floors:{floorId}";
        string? svgSuffix = workspaceTexts is not null ? "wt" : null;
        string fileName = $"floor-{floorId}.svg";

        ImageResult result;
        try
        {
            result = await _imageService.GetSvgResultAsync(
                cacheKey,
                fetchOriginal: async ct => await FetchAndCleanSvgAsync(floorId, workspaceTexts, ct).ConfigureAwait(false),
                svgSuffix: svgSuffix,
                cancellationToken).ConfigureAwait(false);
        }
        catch (SyntheticTimeoutException ex)
        {
            _logger.LogWarning("Blueprint generation timed out for floor {FloorId}. The result will be cached in the background for the next request.", floorId);
            throw new FloorBlueprintUnavailableException($"Blueprint generation for floor {floorId} timed out. Try again shortly — the result is being generated in the background.", ex);
        }

        return new FloorBlueprint(new MemoryStream(result.Data), result.ContentType, fileName, result.IsGzipped);
    }

    private async Task<byte[]> FetchAndCleanSvgAsync(int floorId, IDictionary<int, IReadOnlyList<string>>? workspaceTexts, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await _pythagorasClient
                .GetFloorBlueprintAsync(floorId, BlueprintFormat.Svg, workspaceTexts, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "HTTP error while retrieving SVG blueprint for floor {FloorId}", floorId);
            throw new FloorBlueprintUnavailableException("Pythagoras HTTP request failed.", ex);
        }

        using (response)
        {
            await ThrowIfNotSuccessAsync(response, floorId, cancellationToken).ConfigureAwait(false);

            byte[] rawBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            return CleanSvgBytes(rawBytes);
        }
    }

    private byte[] CleanSvgBytes(byte[] rawBytes)
    {
        try
        {
            using MemoryStream input = new(rawBytes);
            XDocument document = XDocument.Load(input);
            SvgCleaner.RemoveNodes(document, _nodesToRemove);
            document = SvgCleaner.CropSvgToContent(document);
            document = SvgCleaner.NormalizeFontSizes(document);

            using MemoryStream output = new();
            document.Save(output, SaveOptions.DisableFormatting | SaveOptions.OmitDuplicateNamespaces);
            return output.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "Failed to clean SVG blueprint, returning original.");
            return rawBytes;
        }
    }

    private async Task<FloorBlueprint> FetchBlueprintFromPythagorasAsync(int floorId, BlueprintFormat format, IDictionary<int, IReadOnlyList<string>>? workspaceTexts, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await _pythagorasClient
                .GetFloorBlueprintAsync(floorId, format, workspaceTexts, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "HTTP error while retrieving blueprint for floor {FloorId}", floorId);
            throw new FloorBlueprintUnavailableException("Pythagoras HTTP request failed.", ex);
        }

        using (response)
        {
            await ThrowIfNotSuccessAsync(response, floorId, cancellationToken).ConfigureAwait(false);

            using Stream responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            MemoryStream buffer = new();
            await responseStream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
            buffer.Position = 0;

            string contentType = ResolveContentType(format, response.Content.Headers.ContentType?.MediaType);
            string resolvedFileName = TryResolveFileName(response.Content.Headers.ContentDisposition);
            string fileName = EnsureFileName(resolvedFileName, floorId, format);

            return new FloorBlueprint(buffer, contentType, fileName);
        }
    }

    private async Task ThrowIfNotSuccessAsync(HttpResponseMessage response, int floorId, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        string errorDescription = await ReadErrorBodyAsync(response, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning(
                "Blueprint not found for floor {FloorId} in Pythagoras. Body: {Body}",
                floorId,
                errorDescription);

            throw new KeyNotFoundException($"Blueprint for floor {floorId} was not found.");
        }

        _logger.LogWarning(
            "Blueprint request returned {StatusCode} for floor {FloorId}. Body: {Body}",
            (int)response.StatusCode,
            floorId,
            errorDescription);

        string reason = string.IsNullOrWhiteSpace(response.ReasonPhrase)
            ? response.StatusCode.ToString()
            : response.ReasonPhrase;

        throw new FloorBlueprintUnavailableException($"Pythagoras returned {(int)response.StatusCode} ({reason}). Body: {errorDescription}");
    }

    private static string EnsureFileName(string fileName, int floorId, BlueprintFormat format)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return $"floor-{floorId}.{FormatToExtension(format)}";
        }

        if (Path.HasExtension(fileName))
        {
            return fileName;
        }

        return $"{fileName}.{FormatToExtension(format)}";
    }

    private static string ResolveWorkspaceText(RoomModel room)
    {
        if (!string.IsNullOrWhiteSpace(room.PopularName))
        {
            return room.PopularName!;
        }

        if (!string.IsNullOrWhiteSpace(room.Name))
        {
            return room.Name;
        }

        return string.Empty;
    }

    private static string TryResolveFileName(ContentDispositionHeaderValue? header)
    {
        if (header is null)
        {
            return "blueprint";
        }

        if (!string.IsNullOrWhiteSpace(header.FileNameStar))
        {
            return header.FileNameStar.Trim('"');
        }

        if (!string.IsNullOrWhiteSpace(header.FileName))
        {
            return header.FileName.Trim('"');
        }

        return "blueprint";
    }

    private static string FormatToExtension(BlueprintFormat format) => format switch
    {
        BlueprintFormat.Pdf => "pdf",
        BlueprintFormat.Svg => "svg",
        _ => "file"
    };

    private static async Task<string> ReadErrorBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return NormalizeErrorBody(body);
        }
        catch (Exception)
        {
            return "Failed to read error response body.";
        }
    }

    private static string NormalizeErrorBody(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return "No response body.";
        }

        string singleLine = body.ReplaceLineEndings(" ").Trim();
        const int maxLength = 500;
        if (singleLine.Length <= maxLength)
        {
            return singleLine;
        }

        return $"{singleLine[..maxLength]}…";
    }

    private static string ResolveContentType(BlueprintFormat format, string? fallback)
    {
        return format switch
        {
            BlueprintFormat.Pdf => "application/pdf",
            BlueprintFormat.Svg => "image/svg+xml",
            _ when !string.IsNullOrWhiteSpace(fallback) => fallback!,
            _ => "application/octet-stream"
        };
    }
}
