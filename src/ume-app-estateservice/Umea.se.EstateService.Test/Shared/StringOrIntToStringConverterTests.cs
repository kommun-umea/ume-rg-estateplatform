using System.Text.Json;
using Umea.se.EstateService.Shared.Json;

namespace Umea.se.EstateService.Test.Shared;

public class StringOrIntToStringConverterTests
{
    private readonly StringOrIntToStringConverter _converter = new();

    [Theory]
    [InlineData("\"hello\"", "hello")]
    [InlineData("\"123\"", "123")]
    [InlineData("123", "123")]
    [InlineData("0", "0")]
    public void Read_Should_Convert_String_Or_Int_To_String(string json, string expected)
    {
        Utf8JsonReader reader = new(System.Text.Encoding.UTF8.GetBytes(json));
        reader.Read();
        string? result = _converter.Read(ref reader, typeof(string), new JsonSerializerOptions());
        result.ShouldBe(expected);
    }

    [Fact]
    public void Read_Should_Return_Null_For_Json_Null()
    {
        string json = "null";
        Utf8JsonReader reader = new(System.Text.Encoding.UTF8.GetBytes(json));
        reader.Read();
        string? result = _converter.Read(ref reader, typeof(string), new JsonSerializerOptions());
        result.ShouldBeNull();
    }

    [Theory]
    [InlineData("hello", "\"hello\"")]
    [InlineData("123", "\"123\"")]
    [InlineData(null, "null")]
    public void Write_Should_Serialize_String_As_Json(string? value, string expectedJson)
    {
        JsonSerializerOptions options = new();
        using MemoryStream stream = new();
        using Utf8JsonWriter writer = new(stream);
        _converter.Write(writer, value, options);
        writer.Flush();
        string json = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        json.ShouldBe(expectedJson);
    }
}
