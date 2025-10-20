using System.Security.Claims;
using Umea.se.EstateService.Shared.Infrastructure;

namespace Umea.se.EstateService.API.Authorization;

public static class EmployeeClaimEvaluator
{
    private static readonly string[] IdpClaimTypes =
    [
        "idp",
        "http://schemas.microsoft.com/identity/claims/identityprovider",
    ];

    private static readonly string[] ScopeClaimTypes =
    [
        "scope",
        "scp",
        "http://schemas.microsoft.com/identity/claims/scope",
    ];

    public static bool IsEmployee(ClaimsPrincipal? user, ApplicationConfig config)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(config.EmployeeClaimValue))
        {
            return false;
        }

        string? identityProvider = GetFirstNonEmptyValue(user, IdpClaimTypes);
        if (string.IsNullOrWhiteSpace(identityProvider)
            || !string.Equals(identityProvider, config.EmployeeIdpClaimValue, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return HasScope(user, config.EmployeeClaimValue);
    }

    private static string? GetFirstNonEmptyValue(ClaimsPrincipal user, IEnumerable<string> claimTypes)
    {
        foreach (string claimType in claimTypes)
        {
            string? value = user.FindFirst(claimType)?.Value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static bool HasScope(ClaimsPrincipal user, string expectedScope)
    {
        foreach (string claimType in ScopeClaimTypes)
        {
            foreach (Claim claim in user.FindAll(claimType))
            {
                if (ScopeMatches(claim.Value, expectedScope))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool ScopeMatches(string? claimValue, string expectedScope)
    {
        if (string.IsNullOrWhiteSpace(claimValue))
        {
            return false;
        }

        if (string.Equals(claimValue, expectedScope, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        foreach (string segment in claimValue.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries))
        {
            if (string.Equals(segment, expectedScope, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
