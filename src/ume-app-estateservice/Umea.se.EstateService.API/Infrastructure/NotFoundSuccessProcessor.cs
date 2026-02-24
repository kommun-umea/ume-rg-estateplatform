using System.Diagnostics;
using System.Text.RegularExpressions;
using OpenTelemetry;

namespace Umea.se.EstateService.API.Infrastructure;

/// <summary>
/// Marks 404 responses as successful for endpoints where "not found" is expected behavior.
/// This prevents expected 404s (e.g., building has no image) from appearing as errors in Application Insights.
/// </summary>
public partial class NotFoundSuccessProcessor : BaseProcessor<Activity>
{
    [GeneratedRegex(@"^/api/v[\d.]+/buildings/\d+/images?$", RegexOptions.IgnoreCase)]
    private static partial Regex BuildingImagesPattern();

    public override void OnEnd(Activity activity)
    {
        if (activity.Kind != ActivityKind.Server)
        {
            return;
        }

        string? statusCode = activity.GetTagItem("http.response.status_code")?.ToString();
        if (statusCode != "404")
        {
            return;
        }

        string? path = activity.GetTagItem("url.path")?.ToString();
        if (path is not null && BuildingImagesPattern().IsMatch(path))
        {
            activity.SetStatus(ActivityStatusCode.Ok);
        }
    }
}
