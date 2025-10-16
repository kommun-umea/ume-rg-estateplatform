using Microsoft.AspNetCore.Authorization;

namespace Umea.se.EstateService.API.Authorization;

public sealed class EmployeeOrApiKeyRequirement(IReadOnlyCollection<string> allowedApiKeyNames) : IAuthorizationRequirement
{
    public IReadOnlyCollection<string> AllowedApiKeyNames { get; } = allowedApiKeyNames;
}
