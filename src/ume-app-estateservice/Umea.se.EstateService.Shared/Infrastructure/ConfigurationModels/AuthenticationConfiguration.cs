namespace Umea.se.EstateService.Shared.Infrastructure.ConfigurationModels;

public class AuthenticationConfiguration
{
    public required string TokenServiceUrl { get; set; }
    public required string Audience { get; set; }
}
