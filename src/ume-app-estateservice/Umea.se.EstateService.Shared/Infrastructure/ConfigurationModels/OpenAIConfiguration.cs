namespace Umea.se.EstateService.Shared.Infrastructure.ConfigurationModels;

public class OpenAIConfiguration
{
    public bool Enabled { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-5-nano";
}
