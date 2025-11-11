using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.Logic.Models;
using Umea.se.EstateService.Logic.Exceptions;
using Umea.se.EstateService.Logic.Helpers;
using Umea.se.EstateService.ServiceAccess.Common;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enum;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Logic.Handlers;

public sealed class FloorBlueprintHandler(IPythagorasClient pythagorasClient, IPythagorasHandler pythagorasHandler, ILogger<FloorBlueprintHandler> logger) : IFloorBlueprintService
{
    private static readonly string[] _nodesToRemove =
    [
        "svgPageBorder",
        "svgSignature",
        "svgStamp"
    ];

    private readonly IPythagorasClient _pythagorasClient = pythagorasClient;
    private readonly IPythagorasHandler _pythagorasHandler = pythagorasHandler;
    private readonly ILogger<FloorBlueprintHandler> _logger = logger;

    public async Task<FloorBlueprint> GetBlueprintAsync(int floorId, BlueprintFormat format, bool includeWorkspaceTexts, CancellationToken cancellationToken = default)
    {
        includeWorkspaceTexts = true;

        if (floorId <= 0)
        {
            throw new FloorBlueprintValidationException("Floor id must be positive.");
        }

        IDictionary<int, IReadOnlyList<string>>? workspaceTexts = null;

        if (includeWorkspaceTexts)
        {
            _logger.LogInformation("Workspace text enrichment requested for floor {FloorId}. Fetching room data.", floorId);

            IReadOnlyList<RoomModel> rooms = await _pythagorasHandler
                .GetRoomsAsync(roomIds: null, buildingId: null, floorId: floorId, queryArgs: null, cancellationToken)
                .ConfigureAwait(false);

            if (rooms.Count > 0)
            {
                workspaceTexts = rooms.ToDictionary(
                    static room => room.Id,
                    static room => (IReadOnlyList<string>)[ResolveWorkspaceText(room)]);

                _logger.LogInformation("Found {Count} rooms to include in blueprint for floor {FloorId}.", rooms.Count, floorId);
            }
            else
            {
                _logger.LogWarning("Workspace text enrichment requested, but no rooms found for floor {FloorId}.", floorId);
            }
        }

        BinaryResourceResult? resource;
        try
        {
            resource = await _pythagorasClient
                .GetFloorBlueprintAsync(floorId, format, workspaceTexts, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "HTTP error while retrieving blueprint for floor {FloorId}", floorId);
            throw new FloorBlueprintUnavailableException("Pythagoras HTTP request failed.", ex);
        }

        if (resource is null)
        {
            _logger.LogWarning("Blueprint not found for floor {FloorId} in Pythagoras.", floorId);
            throw new KeyNotFoundException($"Blueprint for floor {floorId} was not found.");
        }

        await using (resource.ConfigureAwait(false))
        {
            using Stream responseStream = resource.Content;
            MemoryStream buffer = new();
            await responseStream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
            buffer.Position = 0;

            if (format == BlueprintFormat.Svg)
            {
                MemoryStream? cleaned = TryCleanSvg(buffer);
                if (cleaned is not null)
                {
                    buffer = cleaned;
                }
                else
                {
                    buffer.Position = 0;
                }
            }

            string contentType = ResolveContentType(format, resource.ContentType);
            string resolvedFileName = resource.FileName ?? "blueprint";
            string fileName = EnsureFileName(resolvedFileName, floorId, format);

            return new FloorBlueprint(buffer, contentType, fileName);
        }
    }

    private MemoryStream? TryCleanSvg(MemoryStream source)
    {
        try
        {
            source.Position = 0;
            XDocument document = XDocument.Load(source);
            SvgCleaner.RemoveNodes(document, _nodesToRemove);
            document = SvgCleaner.CropSvgToContent(document);
            document = SvgCleaner.NormalizeFontSizes(document);

            MemoryStream cleaned = new();
            document.Save(cleaned, SaveOptions.DisableFormatting | SaveOptions.OmitDuplicateNamespaces);
            cleaned.Position = 0;
            return cleaned;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clean SVG blueprint before returning result.");
            return null;
        }
        finally
        {
            source.Position = 0;
        }
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

    private static string FormatToExtension(BlueprintFormat format) => format switch
    {
        BlueprintFormat.Pdf => "pdf",
        BlueprintFormat.Svg => "svg",
        _ => "file"
    };

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
