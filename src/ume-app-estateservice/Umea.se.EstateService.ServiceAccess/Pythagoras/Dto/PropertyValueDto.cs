using System.Text.Json.Serialization;

namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;

public sealed class PropertyValueDto
{
    [JsonPropertyName("value")]
    public string? Value { get; init; }
}
