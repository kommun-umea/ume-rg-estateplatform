using System.Diagnostics;
using OpenTelemetry;

namespace Umea.se.EstateService.API.Infrastructure;

/// <summary>
/// Marks 401 and 404 responses as successful so they don't appear as errors in Application Insights.
/// These are expected HTTP responses (unauthorized access, missing resources), not application failures.
/// </summary>
public class HttpStatusSuccessProcessor : BaseProcessor<Activity>
{
    private static readonly HashSet<string> _expectedStatusCodes = ["401", "404"];

    public override void OnEnd(Activity activity)
    {
        if (activity.Kind != ActivityKind.Server)
        {
            return;
        }

        string? statusCode = activity.GetTagItem("http.response.status_code")?.ToString();
        if (statusCode is not null && _expectedStatusCodes.Contains(statusCode))
        {
            activity.SetStatus(ActivityStatusCode.Unset);
        }
    }
}
