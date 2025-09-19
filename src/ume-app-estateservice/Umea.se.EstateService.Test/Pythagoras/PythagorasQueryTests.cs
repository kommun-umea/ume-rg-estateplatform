using System.Text.Json.Serialization;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;

namespace Umea.se.EstateService.Test.Pythagoras;

public class PythagorasQueryTests
{
    [Fact]
    public void BuildAsQueryString_WithIds_AddsEachIdParameter()
    {
        PythagorasQuery<SampleDto> query = new();

        query.WithIds(1, 2, 3);

        Dictionary<string, List<string>> dict = Parse(query.BuildAsQueryString());

        Assert.Equal(["1", "2", "3"], dict["id[]"]);
        Assert.Single(dict);
    }

    [Fact]
    public void BuildAsQueryString_WithGeneralSearch_AddsGeneralSearchParameter()
    {
        PythagorasQuery<SampleDto> query = new();

        query.GeneralSearch("office");

        Dictionary<string, List<string>> dict = Parse(query.BuildAsQueryString());

        Assert.Equal("office", Assert.Single(dict["generalSearch"]));
    }

    [Fact]
    public void BuildAsQueryString_WithParameterFilter_UsesCamelCasedName()
    {
        PythagorasQuery<SampleDto> query = new();

        query.Where(x => x.DisplayName, Op.Eq, "Main entrance");

        Dictionary<string, List<string>> dict = Parse(query.BuildAsQueryString());

        Assert.Equal("EQ:displayName", Assert.Single(dict["pN[]"]));
        Assert.Equal("Main entrance", Assert.Single(dict["pV[]"]));
    }

    [Fact]
    public void BuildAsQueryString_WithAttributeTarget_UsesJsonPropertyName()
    {
        PythagorasQuery<SampleDto> query = new();

        query.WhereAttribute(x => x.CustomLabel, "likeaw", "north");

        Dictionary<string, List<string>> dict = Parse(query.BuildAsQueryString());

        Assert.Equal("LIKEAW:custom_label", Assert.Single(dict["aN[]"]));
        Assert.Equal("north", Assert.Single(dict["aV[]"]));
    }

    [Fact]
    public void BuildAsQueryString_WithContains_IsCaseInsensitiveLike()
    {
        PythagorasQuery<SampleDto> query = new();

        query.Contains(x => x.DisplayName, "north");

        Dictionary<string, List<string>> dict = Parse(query.BuildAsQueryString());

        Assert.Equal("ILIKEAW:displayName", Assert.Single(dict["pN[]"]));
        Assert.Equal("north", Assert.Single(dict["pV[]"]));
    }

    [Fact]
    public void BuildAsQueryString_OrderByAscending_AddsOrderByParameters()
    {
        PythagorasQuery<SampleDto> query = new();

        query.OrderBy(x => x.CreatedAt);

        Dictionary<string, List<string>> dict = Parse(query.BuildAsQueryString());

        Assert.Equal("createdAt", Assert.Single(dict["orderBy"]));
        Assert.Equal("true", Assert.Single(dict["orderAsc"]));
    }

    [Fact]
    public void BuildAsQueryString_OrderByDescending_SetsOrderAscFalse()
    {
        PythagorasQuery<SampleDto> query = new();

        query.OrderByDescending(x => x.CreatedAt);

        Dictionary<string, List<string>> dict = Parse(query.BuildAsQueryString());

        Assert.Equal("createdAt", Assert.Single(dict["orderBy"]));
        Assert.Equal("false", Assert.Single(dict["orderAsc"]));
    }

    [Fact]
    public void BuildAsQueryString_SkipAndTake_ComposePagingParameters()
    {
        PythagorasQuery<SampleDto> query = new();

        query.Skip(5).Take(10);

        Dictionary<string, List<string>> dict = Parse(query.BuildAsQueryString());

        Assert.Equal("5", Assert.Single(dict["firstResult"]));
        Assert.Equal("10", Assert.Single(dict["maxResults"]));
    }

    [Fact]
    public void BuildAsQueryString_Page_ComputesFirstResultAndSize()
    {
        PythagorasQuery<SampleDto> builder = new();

        builder.Page(2, 25);

        Dictionary<string, List<string>> dict = Parse(builder.BuildAsQueryString());

        Assert.Equal("25", Assert.Single(dict["firstResult"]));
        Assert.Equal("25", Assert.Single(dict["maxResults"]));
    }

    [Fact]
    public void Page_Throws_WhenPageNumberInvalid()
    {
        PythagorasQuery<SampleDto> query = new();

        ArgumentException ex = Assert.Throws<ArgumentException>(() => query.Page(0, 10));
        Assert.Equal("pageNumber", ex.ParamName);
    }

    [Fact]
    public void Page_Throws_WhenPageSizeInvalid()
    {
        PythagorasQuery<SampleDto> query = new();

        ArgumentException ex = Assert.Throws<ArgumentException>(() => query.Page(1, 0));
        Assert.Equal("pageSize", ex.ParamName);
    }

    [Fact]
    public void Page_Throws_WhenUsedAfterSkip()
    {
        PythagorasQuery<SampleDto> query = new();
        query.Skip(5);

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => query.Page(1, 10));
        Assert.Contains("Page()", ex.Message);
    }

    [Fact]
    public void Skip_Throws_WhenUsedAfterPage()
    {
        PythagorasQuery<SampleDto> query = new();
        query.Page(1, 10);

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => query.Skip(5));
        Assert.Contains("Skip()", ex.Message);
    }

    [Fact]
    public void Take_Throws_WhenUsedAfterPage()
    {
        PythagorasQuery<SampleDto> query = new();
        query.Page(1, 10);

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => query.Take(5));
        Assert.Contains("Take()", ex.Message);
    }

    [Fact]
    public void Page_Throws_WhenUsedAfterTake()
    {
        PythagorasQuery<SampleDto> query = new();
        query.Take(10);

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => query.Page(1, 10));
        Assert.Contains("Page()", ex.Message);
    }

    [Fact]
    public void BuildAsQueryString_Between_AddsRangeFilters()
    {
        PythagorasQuery<SampleDto> query = new();

        query.Between(x => x.Level, 1, 5);

        Dictionary<string, List<string>> dict = Parse(query.BuildAsQueryString());

        Assert.Equal(["GE:level", "LE:level"], dict["pN[]"]);
        Assert.Equal(["1", "5"], dict["pV[]"]);
    }

    [Fact]
    public void BuildAsQueryString_DateTimesAreFormattedInIso8601Utc()
    {
        DateTime value = new(2024, 2, 3, 4, 5, 6, DateTimeKind.Unspecified);
        PythagorasQuery<SampleDto> query = new();

        query.Where(x => x.CreatedAt, value);

        Dictionary<string, List<string>> dict = Parse(query.BuildAsQueryString());

        string serialized = Assert.Single(dict["pV[]"]);
        Assert.EndsWith("Z", serialized);
        Assert.Equal(DateTime.SpecifyKind(value, DateTimeKind.Utc).ToString("o"), serialized);
    }

    [Fact]
    public void BuildAsQueryString_Throws_WhenIdsCombinedWithGeneralSearch()
    {
        PythagorasQuery<SampleDto> query = new();
        query.WithIds(1);
        query.GeneralSearch("conflict");

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(query.BuildAsQueryString);

        Assert.Contains("WithIds", ex.Message);
    }

    [Fact]
    public void BuildAsQueryString_Throws_WhenConflictingFilters()
    {
        PythagorasQuery<SampleDto> query = new();
        query.Where(x => x.Level, Op.Gt, 1);
        query.Where(x => x.Level, Op.Gt, 2);

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(query.BuildAsQueryString);

        Assert.Contains("Conflicting filters", ex.Message);
    }

    [Fact]
    public void BuildAsQueryString_AllowsSeparateTargetsForSameProperty()
    {
        PythagorasQuery<SampleDto> query = new();

        query.WhereParameter(x => x.DisplayName, "=", "inside");
        query.WhereAttribute(x => x.DisplayName, "=", "outside");

        Dictionary<string, List<string>> dict = Parse(query.BuildAsQueryString());

        Assert.Contains("EQ:displayName", dict["pN[]"]);
        Assert.Contains("EQ:displayName", dict["aN[]"]);
        Assert.Contains("inside", dict["pV[]"]);
        Assert.Contains("outside", dict["aV[]"]);
    }

    [Fact]
    public void Build_AppendsQueryToBaseUrl()
    {
        PythagorasQuery<SampleDto> query = new();
        query.GeneralSearch("north");

        HttpRequestMessage request = query.Build(HttpMethod.Get, "https://example.test/rest/v1/buildings");

        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("https://example.test/rest/v1/buildings?generalSearch=north", request.RequestUri!.ToString());
    }

    [Fact]
    public void Build_AppendsQueryToExistingQueryString()
    {
        PythagorasQuery<SampleDto> query = new();
        query.WithIds(42);

        HttpRequestMessage request = query.Build(HttpMethod.Get, "https://example.test/api?existing=1");

        Assert.Equal("https://example.test/api?existing=1&id%5B%5D=42", request.RequestUri!.ToString());
    }

    [Fact]
    public void Build_WhenNoQuery_ReturnsBaseUrl()
    {
        PythagorasQuery<SampleDto> query = new();

        HttpRequestMessage request = query.Build(HttpMethod.Get, "https://example.test/rest/v1/buildings");

        Assert.Equal("https://example.test/rest/v1/buildings", request.RequestUri!.ToString());
    }

    [Fact]
    public void Clone_CopiesUnderlyingRequest()
    {
        PythagorasQuery<SampleDto> original = new();
        original.GeneralSearch("foo");

        PythagorasQuery<SampleDto> clone = original.Clone();

        Assert.Equal(original.BuildAsQueryString(), clone.BuildAsQueryString());

        clone.GeneralSearch("bar");
        Assert.NotEqual(original.BuildAsQueryString(), clone.BuildAsQueryString());
    }

    [Fact]
    public void Clone_PreservesSkipTakeFlags()
    {
        PythagorasQuery<SampleDto> original = new();
        original.Skip(5);

        PythagorasQuery<SampleDto> clone = original.Clone();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => clone.Page(1, 10));
        Assert.Contains("Page()", ex.Message);
    }

    private sealed class SampleDto
    {
        public int Level { get; init; }
        public string DisplayName { get; init; } = string.Empty;

        [JsonPropertyName("custom_label")]
        public string CustomLabel { get; init; } = string.Empty;

        public DateTime CreatedAt { get; init; }
    }

    private static Dictionary<string, List<string>> Parse(string query)
    {
        Dictionary<string, List<string>> result = new(StringComparer.Ordinal);

        if (string.IsNullOrEmpty(query))
        {
            return result;
        }

        string[] pairs = query.Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (string pair in pairs)
        {
            string[] kv = pair.Split('=', 2);
            string key = Uri.UnescapeDataString(kv[0]);
            string value = kv.Length > 1 ? Uri.UnescapeDataString(kv[1]) : string.Empty;

            if (!result.TryGetValue(key, out List<string>? list))
            {
                list = [];
                result[key] = list;
            }

            list.Add(value);
        }

        return result;
    }
}
