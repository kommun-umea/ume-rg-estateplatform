using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;

internal sealed class UnixMillisDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.Number)
        {
            throw new JsonException("Unix timestamp must be a number.");
        }

        long milliseconds = reader.GetInt64();
        return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime;
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        long milliseconds = new DateTimeOffset(value.ToUniversalTime()).ToUnixTimeMilliseconds();
        writer.WriteNumberValue(milliseconds);
    }
}
