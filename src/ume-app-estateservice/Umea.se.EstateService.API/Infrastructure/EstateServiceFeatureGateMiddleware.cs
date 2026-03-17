using Microsoft.FeatureManagement;

namespace Umea.se.EstateService.API.Infrastructure;

public class EstateServiceFeatureGateMiddleware(RequestDelegate next, IVariantFeatureManager featureManager)
{
    private static readonly HashSet<string> _excludedPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/v1.0/home",
        "/api/v1.0/admin",
        "/api/v1.0/features",
        "/api/v1.0/cache",
        "/swagger",
    };

    public async Task InvokeAsync(HttpContext context)
    {
        string path = context.Request.Path.Value ?? string.Empty;

        bool excluded = false;
        foreach (string prefix in _excludedPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                excluded = true;
                break;
            }
        }

        if (!excluded && !await featureManager.IsEnabledAsync("EstateService"))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        await next(context);
    }
}
