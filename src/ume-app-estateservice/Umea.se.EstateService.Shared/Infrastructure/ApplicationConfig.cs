using Microsoft.Extensions.Configuration;
using Umea.se.Toolkit.Auth;
using Umea.se.Toolkit.Configuration;

namespace Umea.se.EstateService.Shared.Infrastructure;

public class ApplicationConfig(IConfiguration configuration) : ApplicationConfigCloudBase(configuration), IEmployeeAuthorizationConfig
{
    public string PythagorasApiKey => GetValue("Pythagoras-Api-Key");
    public string PythagorasBaseUrl => GetValue("Pythagoras-Base-Url");
    public string TokenServiceAddress => GetValue("TokenService:Address");
    public string EmployeeIdpClaimValue => TryGetValue<string>("Authentication:Employee:IdpClaimValue") ?? "AzureActiveDirectory";
    public string? EmployeeClaimValue => TryGetValue<string>("Authentication:Employee:Claim:Value") ?? "internal:mypage";
}
