using System.Security.Claims;
using Umea.se.EstateService.Shared.Infrastructure;

namespace Umea.se.EstateService.API.Authorization;

public static class EmployeeClaimEvaluator
{
    public static bool IsEmployee(ClaimsPrincipal? user, ApplicationConfig config)
    {
        if (user is null)
        {
            return false;
        }

        string? idpClaimValue = user.FindFirstValue("idp");
        if (!string.IsNullOrWhiteSpace(idpClaimValue) && string.Equals(idpClaimValue, config.EmployeeIdpClaimValue, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(config.EmployeeClaimType) || string.IsNullOrWhiteSpace(config.EmployeeClaimValue))
        {
            return false;
        }

        string? employeeClaim = user.FindFirstValue(config.EmployeeClaimType);
        return !string.IsNullOrWhiteSpace(employeeClaim) && string.Equals(employeeClaim, config.EmployeeClaimValue, StringComparison.OrdinalIgnoreCase);
    }
}
