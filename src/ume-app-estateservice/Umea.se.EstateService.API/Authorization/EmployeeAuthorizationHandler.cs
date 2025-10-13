using Microsoft.AspNetCore.Authorization;
using Umea.se.EstateService.Shared.Infrastructure;

namespace Umea.se.EstateService.API.Authorization;

public sealed class EmployeeAuthorizationHandler(
    ApplicationConfig config,
    ILogger<EmployeeAuthorizationHandler> logger) : AuthorizationHandler<EmployeeRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, EmployeeRequirement requirement)
    {
        if (EmployeeClaimEvaluator.IsEmployee(context.User, config))
        {
            context.Succeed(requirement);
        }
        else
        {
            logger.LogWarning("Authorization failed because the user did not satisfy employee requirements.");
        }

        return Task.CompletedTask;
    }
}
