namespace Umea.se.EstateService.Shared.Infrastructure.ConfigurationModels;

public class AuthenticationConfiguration
{
    public required string TokenServiceUrl { get; init; }
    public required string Audience { get; init; }
}
