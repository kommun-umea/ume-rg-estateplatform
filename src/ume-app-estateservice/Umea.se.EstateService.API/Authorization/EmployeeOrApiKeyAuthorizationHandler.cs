using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using Umea.se.EstateService.Shared.Infrastructure;

namespace Umea.se.EstateService.API.Authorization;

public sealed class EmployeeOrApiKeyAuthorizationHandler(
    ApplicationConfig config,
    ILogger<EmployeeOrApiKeyAuthorizationHandler> logger) : AuthorizationHandler<EmployeeOrApiKeyRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, EmployeeOrApiKeyRequirement requirement)
    {
        if (EmployeeClaimEvaluator.IsEmployee(context.User, config))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (!TryGetHttpContext(context, out HttpContext? httpContext))
        {
            logger.LogWarning("Authorization failed because the HttpContext was unavailable.");
            return Task.CompletedTask;
        }

        if (!httpContext!.Request.Headers.TryGetValue("X-Api-Key", out StringValues values))
        {
            logger.LogWarning("Authorization failed because no API key was provided.");
            return Task.CompletedTask;
        }

        string apiKeyValue = values.ToString();
        if (string.IsNullOrWhiteSpace(apiKeyValue))
        {
            logger.LogWarning("Authorization failed because the API key header was empty.");
            return Task.CompletedTask;
        }

        string? matchedApiKeyName = config.ApiKeys.FirstOrDefault(pair => string.Equals(pair.Value, apiKeyValue, StringComparison.Ordinal)).Key;
        if (matchedApiKeyName is null)
        {
            logger.LogWarning("Authorization failed because the API key did not match any configured keys.");
            return Task.CompletedTask;
        }

        if (requirement.AllowedApiKeyNames.Count > 0 && !requirement.AllowedApiKeyNames.Contains(matchedApiKeyName, StringComparer.Ordinal))
        {
            logger.LogWarning("Authorization failed because API key '{ApiKeyName}' is not permitted for this policy.", matchedApiKeyName);
            return Task.CompletedTask;
        }

        context.Succeed(requirement);
        return Task.CompletedTask;
    }

    private static bool TryGetHttpContext(AuthorizationHandlerContext context, out HttpContext? httpContext)
    {
        httpContext = context.Resource switch
        {
            HttpContext direct => direct,
            AuthorizationFilterContext filterContext => filterContext.HttpContext,
            _ => null
        };

        return httpContext is not null;
    }
}
