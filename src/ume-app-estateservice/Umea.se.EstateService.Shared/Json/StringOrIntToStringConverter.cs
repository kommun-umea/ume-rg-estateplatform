using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umea.se.EstateService.Shared.Json;

public class StringOrIntToStringConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => reader.GetInt32().ToString(CultureInfo.InvariantCulture),
            JsonTokenType.Null => null,
            _ => throw new JsonException($"Unexpected token type: {reader.TokenType}"),
        };
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStringValue(value);
        }
    }
}
