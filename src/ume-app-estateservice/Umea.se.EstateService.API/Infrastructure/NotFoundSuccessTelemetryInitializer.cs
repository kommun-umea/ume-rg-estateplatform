using System.Text.RegularExpressions;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Umea.se.EstateService.API.Infrastructure;

/// <summary>
/// Marks 404 responses as successful for endpoints where "not found" is expected behavior.
/// This prevents expected 404s (e.g., building has no image) from appearing as errors in Application Insights.
/// </summary>
public partial class NotFoundSuccessTelemetryInitializer : ITelemetryInitializer
{
    [GeneratedRegex(@"^/api/v[\d.]+/buildings/\d+/images?$", RegexOptions.IgnoreCase)]
    private static partial Regex BuildingImagesPattern();

    public void Initialize(ITelemetry telemetry)
    {
        if (telemetry is not RequestTelemetry request)
        {
            return;
        }

        if (request.ResponseCode != "404")
        {
            return;
        }

        // Check if this is a building images endpoint where 404 is expected
        if (request.Url is not null && BuildingImagesPattern().IsMatch(request.Url.AbsolutePath))
        {
            request.Success = true;
        }
    }
}
