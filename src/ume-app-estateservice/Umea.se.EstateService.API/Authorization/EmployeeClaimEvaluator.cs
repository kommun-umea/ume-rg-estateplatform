using System.IdentityModel.Tokens.Jwt;
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

        string? idpClaimValue = FindFirstValue(user, "idp");
        if (string.IsNullOrWhiteSpace(idpClaimValue) || !string.Equals(idpClaimValue, config.EmployeeIdpClaimValue, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(config.EmployeeClaimValue))
        {
            return false;
        }

        return FindAllValues(user, "scope")
            .Any(claim => ClaimValueMatches(claim, config.EmployeeClaimValue));
    }

    private static string? FindFirstValue(ClaimsPrincipal user, string claimType)
    {
        foreach (string candidate in ResolveClaimTypes(claimType))
        {
            string? value = user.FindFirstValue(candidate);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static IEnumerable<string> FindAllValues(ClaimsPrincipal user, string claimType)
    {
        foreach (string candidate in ResolveClaimTypes(claimType))
        {
            foreach (Claim claim in user.FindAll(candidate))
            {
                yield return claim.Value;
            }
        }
    }

    private static IEnumerable<string> ResolveClaimTypes(string claimType)
    {
        HashSet<string> claimTypes = new(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(claimType))
        {
            claimTypes.Add(claimType);

            if (JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.TryGetValue(claimType, out string? mappedClaimType))
            {
                claimTypes.Add(mappedClaimType);
            }

            foreach (KeyValuePair<string, string> mapping in JwtSecurityTokenHandler.DefaultInboundClaimTypeMap)
            {
                if (string.Equals(mapping.Value, claimType, StringComparison.OrdinalIgnoreCase))
                {
                    claimTypes.Add(mapping.Key);
                }
            }
        }

        return claimTypes;
    }

    private static bool ClaimValueMatches(string claimValue, string expectedValue)
    {
        if (string.IsNullOrWhiteSpace(claimValue))
        {
            return false;
        }

        if (string.Equals(claimValue, expectedValue, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        foreach (string segment in claimValue.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries))
        {
            if (string.Equals(segment, expectedValue, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
