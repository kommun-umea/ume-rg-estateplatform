using System.Net.Http.Headers;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.Logic.Models;
using Umea.se.EstateService.Logic.Exceptions;
using Umea.se.EstateService.Logic.Helpers;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enum;

namespace Umea.se.EstateService.Logic.Handlers;

public sealed class FloorBlueprintHandler(IPythagorasClient pythagorasClient, ILogger<FloorBlueprintHandler> logger) : IFloorBlueprintService
{
    private static readonly string[] _nodesToRemove =
    [
        "svgPageBorder",
        "svgSignature",
        "svgStamp"
    ];

    private readonly IPythagorasClient _pythagorasClient = pythagorasClient;
    private readonly ILogger<FloorBlueprintHandler> _logger = logger;

    public async Task<FloorBlueprint> GetBlueprintAsync(int floorId, BlueprintFormat format, bool includeWorkspaceTexts, CancellationToken cancellationToken = default)
    {
        if (floorId <= 0)
        {
            throw new FloorBlueprintValidationException("Floor id must be positive.");
        }

        if (includeWorkspaceTexts)
        {
            _logger.LogDebug("Workspace text enrichment requested for floor {FloorId}, but feature is not yet implemented.", floorId);
        }

        HttpResponseMessage response;
        try
        {
            response = await _pythagorasClient
                .GetFloorBlueprintAsync(floorId, format, includeWorkspaceTexts, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "HTTP error while retrieving blueprint for floor {FloorId}", floorId);
            throw new FloorBlueprintUnavailableException("Pythagoras HTTP request failed.", ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                string errorDescription = await ReadErrorBodyAsync(response, cancellationToken).ConfigureAwait(false);
                _logger.LogWarning("Blueprint request returned {StatusCode} for floor {FloorId}. Body: {Body}", (int)response.StatusCode, floorId, errorDescription);

                string reason = string.IsNullOrWhiteSpace(response.ReasonPhrase)
                    ? response.StatusCode.ToString()
                    : response.ReasonPhrase;

                throw new FloorBlueprintUnavailableException($"Pythagoras returned {(int)response.StatusCode} ({reason}). Body: {errorDescription}");
            }

            await using Stream responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
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

            string contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            string resolvedFileName = TryResolveFileName(response.Content.Headers.ContentDisposition);
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
}
