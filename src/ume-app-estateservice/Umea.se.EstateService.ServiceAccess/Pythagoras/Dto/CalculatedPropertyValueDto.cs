using System.Text.Json.Serialization;
using Umea.se.EstateService.Shared.Json;

namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;

public sealed class CalculatedPropertyValueDto : IPythagorasDto
{
    [JsonPropertyName("readonlyValue")]
    public bool ReadonlyValue { get; set; }

    [JsonPropertyName("outputValue")]
    [JsonConverter(typeof(StringOrIntToStringConverter))]
    public string? OutputValue { get; set; }

    [JsonPropertyName("style")]
    public CalculatedPropertyValueStyleDto? Style { get; set; }

    [JsonPropertyName("valid")]
    public bool Valid { get; set; }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }
}

public sealed class CalculatedPropertyValueStyleDto
{
    [JsonPropertyName("horizontalTextAlignment")]
    public string? HorizontalTextAlignment { get; set; }

    [JsonPropertyName("fontSize")]
    public double? FontSize { get; set; }

    [JsonPropertyName("fontBold")]
    public bool? FontBold { get; set; }

    [JsonPropertyName("fontItalic")]
    public bool? FontItalic { get; set; }

    [JsonPropertyName("fontColor")]
    public string? FontColor { get; set; }

    [JsonPropertyName("cellColor")]
    public string? CellColor { get; set; }

    [JsonPropertyName("width")]
    public string? Width { get; set; }

    [JsonPropertyName("height")]
    public double? Height { get; set; }
}
