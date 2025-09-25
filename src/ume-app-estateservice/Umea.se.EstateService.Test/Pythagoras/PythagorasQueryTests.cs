using System.Text.Json.Serialization;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api.Query;

namespace Umea.se.EstateService.Test.Pythagoras;

public class PythagorasQueryTests
{
    [Fact]
    public void BuildAsQueryString_WithIds_AddsEachIdParameter()
    {
        PythagorasQuery<SampleDto> query = new PythagorasQuery<SampleDto>()
            .WithIds(1, 2, 3);

        Dictionary<string, List<string>> dict = Parse(query.BuildAsQueryString());

        dict["id[]"].ShouldBe(["1", "2", "3"]);
        dict.Count.ShouldBe(1);
    }

    [Fact]
    public void BuildAsQueryString_WithGeneralSearch_AddsGeneralSearchParameter()
    {
        PythagorasQuery<SampleDto> query = new PythagorasQuery<SampleDto>()
            .GeneralSearch("office");

        Dictionary<string, List<string>> dict = Parse(query.BuildAsQueryString());

        string generalSearch = dict["generalSearch"].ShouldHaveSingleItem();
        generalSearch.ShouldBe("office");
    }

    [Fact]
    public void BuildAsQueryString_WithParameterFilter_UsesCamelCasedName()
    {
        PythagorasQuery<SampleDto> query = new PythagorasQuery<SampleDto>()
            .Where(x => x.DisplayName, Op.Eq, "Main entrance");

        Dictionary<string, List<string>> dict = Parse(query.BuildAsQueryString());

        string parameterName = dict["pN[]"].ShouldHaveSingleItem();
        parameterName.ShouldBe("EQ:displayName");
        string parameterValue = dict["pV[]"].ShouldHaveSingleItem();
        parameterValue.ShouldBe("Main entrance");
    }

    [Fact]
    public void BuildAsQueryString_WithAttributeTarget_UsesJsonPropertyName()
    {
        PythagorasQuery<SampleDto> query = new PythagorasQuery<SampleDto>()
            .WhereAttribute(x => x.CustomLabel, "likeaw", "north");

        Dictionary<string, List<string>> dict = Parse(query.BuildAsQueryString());

        string attributeName = dict["aN[]"].ShouldHaveSingleItem();
        attributeName.ShouldBe("LIKEAW:custom_label");
        string attributeValue = dict["aV[]"].ShouldHaveSingleItem();
        attributeValue.ShouldBe("north");
    }

    [Fact]
    public void BuildAsQueryString_WithContains_IsCaseInsensitiveLike()
    {
        PythagorasQuery<SampleDto> query = new PythagorasQuery<SampleDto>()
            .Contains(x => x.DisplayName, "north");

        Dictionary<string, List<string>> dict = Parse(query.BuildAsQueryString());

        string parameterName = dict["pN[]"].ShouldHaveSingleItem();
        parameterName.ShouldBe("ILIKEAW:displayName");
        string parameterValue = dict["pV[]"].ShouldHaveSingleItem();
        parameterValue.ShouldBe("north");
    }

    [Fact]
    public void BuildAsQueryString_OrderByAscending_AddsOrderByParameters()
    {
        PythagorasQuery<SampleDto> query = new PythagorasQuery<SampleDto>()
            .OrderBy(x => x.CreatedAt);

        Dictionary<string, List<string>> dict = Parse(query.BuildAsQueryString());

        string orderBy = dict["orderBy"].ShouldHaveSingleItem();
        orderBy.ShouldBe("createdAt");
        string orderAsc = dict["orderAsc"].ShouldHaveSingleItem();
        orderAsc.ShouldBe("true");
    }

    [Fact]
    public void BuildAsQueryString_OrderByDescending_SetsOrderAscFalse()
    {
        PythagorasQuery<SampleDto> query = new PythagorasQuery<SampleDto>()
            .OrderByDescending(x => x.CreatedAt);

        Dictionary<string, List<string>> dict = Parse(query.BuildAsQueryString());

        string orderBy = dict["orderBy"].ShouldHaveSingleItem();
        orderBy.ShouldBe("createdAt");
        string orderAsc = dict["orderAsc"].ShouldHaveSingleItem();
        orderAsc.ShouldBe("false");
    }

    [Fact]
    public void BuildAsQueryString_SkipAndTake_ComposePagingParameters()
    {
        PythagorasQuery<SampleDto> query = new PythagorasQuery<SampleDto>()
            .Skip(5).Take(10);

        Dictionary<string, List<string>> dict = Parse(query.BuildAsQueryString());

        string firstResult = dict["firstResult"].ShouldHaveSingleItem();
        firstResult.ShouldBe("5");
        string maxResults = dict["maxResults"].ShouldHaveSingleItem();
        maxResults.ShouldBe("10");
    }

    [Fact]
    public void BuildAsQueryString_Page_ComputesFirstResultAndSize()
    {
        PythagorasQuery<SampleDto> builder = new PythagorasQuery<SampleDto>()
            .Page(2, 25);

        Dictionary<string, List<string>> dict = Parse(builder.BuildAsQueryString());

        string firstResult = dict["firstResult"].ShouldHaveSingleItem();
        firstResult.ShouldBe("25");
        string maxResults = dict["maxResults"].ShouldHaveSingleItem();
        maxResults.ShouldBe("25");
    }

    [Fact]
    public void Page_Throws_WhenPageNumberInvalid()
    {
        PythagorasQuery<SampleDto> query = new();

        ArgumentException ex = Should.Throw<ArgumentException>(() => query.Page(0, 10));
        ex.ParamName.ShouldBe("pageNumber");
    }

    [Fact]
    public void Page_Throws_WhenPageSizeInvalid()
    {
        PythagorasQuery<SampleDto> query = new();

        ArgumentException ex = Should.Throw<ArgumentException>(() => query.Page(1, 0));
        ex.ParamName.ShouldBe("pageSize");
    }

    [Fact]
    public void Page_Throws_WhenUsedAfterSkip()
    {
        PythagorasQuery<SampleDto> query = new();
        query = query.Skip(5);

        InvalidOperationException ex = Should.Throw<InvalidOperationException>(() => query.Page(1, 10));
        ex.Message.ShouldContain("Page()");
    }

    [Fact]
    public void Skip_Throws_WhenUsedAfterPage()
    {
        PythagorasQuery<SampleDto> query = new();
        query = query.Page(1, 10);

        InvalidOperationException ex = Should.Throw<InvalidOperationException>(() => query.Skip(5));
        ex.Message.ShouldContain("Skip()");
    }

    [Fact]
    public void Take_Throws_WhenUsedAfterPage()
    {
        PythagorasQuery<SampleDto> query = new();
        query = query.Page(1, 10);

        InvalidOperationException ex = Should.Throw<InvalidOperationException>(() => query.Take(5));
        ex.Message.ShouldContain("Take()");
    }

    [Fact]
    public void Page_Throws_WhenUsedAfterTake()
    {
        PythagorasQuery<SampleDto> query = new();
        query = query.Take(10);

        InvalidOperationException ex = Should.Throw<InvalidOperationException>(() => query.Page(1, 10));
        ex.Message.ShouldContain("Page()");
    }

    [Fact]
    public void BuildAsQueryString_Between_AddsRangeFilters()
    {
        PythagorasQuery<SampleDto> query = new PythagorasQuery<SampleDto>()
            .Between(x => x.Level, 1, 5);

        Dictionary<string, List<string>> dict = Parse(query.BuildAsQueryString());

        dict["pN[]"].ShouldBe(["GE:level", "LE:level"]);

        dict["pV[]"].ShouldBe(["1", "5"]);
    }

    [Fact]
    public void BuildAsQueryString_DateTimesAreFormattedInIso8601Utc()
    {
        DateTime value = new(2024, 2, 3, 4, 5, 6, DateTimeKind.Unspecified);
        PythagorasQuery<SampleDto> query = new PythagorasQuery<SampleDto>()
            .Where(x => x.CreatedAt, value);

        Dictionary<string, List<string>> dict = Parse(query.BuildAsQueryString());

        string serialized = dict["pV[]"].ShouldHaveSingleItem();
        serialized.ShouldEndWith("Z");
        serialized.ShouldBe(DateTime.SpecifyKind(value, DateTimeKind.Utc).ToString("o"));
    }

    [Fact]
    public void BuildAsQueryString_Throws_WhenIdsCombinedWithGeneralSearch()
    {
        PythagorasQuery<SampleDto> query = new();
        query = query.WithIds(1);
        query = query.GeneralSearch("conflict");

        InvalidOperationException ex = Should.Throw<InvalidOperationException>(query.BuildAsQueryString);

        ex.Message.ShouldContain("WithIds");
    }

    [Fact]
    public void BuildAsQueryString_Throws_WhenConflictingFilters()
    {
        PythagorasQuery<SampleDto> query = new();
        query = query.Where(x => x.Level, Op.Gt, 1);
        query = query.Where(x => x.Level, Op.Gt, 2);

        InvalidOperationException ex = Should.Throw<InvalidOperationException>(query.BuildAsQueryString);

        ex.Message.ShouldContain("Conflicting filters");
    }

    [Fact]
    public void BuildAsQueryString_AllowsSeparateTargetsForSameProperty()
    {
        PythagorasQuery<SampleDto> query = new();

        query = query.WhereParameter(x => x.DisplayName, "=", "inside");
        query = query.WhereAttribute(x => x.DisplayName, "=", "outside");

        Dictionary<string, List<string>> dict = Parse(query.BuildAsQueryString());

        dict["pN[]"].ShouldContain("EQ:displayName");
        dict["aN[]"].ShouldContain("EQ:displayName");
        dict["pV[]"].ShouldContain("inside");
        dict["aV[]"].ShouldContain("outside");
    }

    [Fact]
    public void Build_AppendsQueryToBaseUrl()
    {
        PythagorasQuery<SampleDto> query = new();
        query = query.GeneralSearch("north");

        HttpRequestMessage request = query.Build(HttpMethod.Get, "https://example.test/rest/v1/buildings");

        request.Method.ShouldBe(HttpMethod.Get);
        request.RequestUri!.ToString().ShouldBe("https://example.test/rest/v1/buildings?generalSearch=north");
    }

    [Fact]
    public void Build_AppendsQueryToExistingQueryString()
    {
        PythagorasQuery<SampleDto> query = new PythagorasQuery<SampleDto>()
            .WithIds(42);

        HttpRequestMessage request = query.Build(HttpMethod.Get, "https://example.test/api?existing=1");

        request.RequestUri!.ToString().ShouldBe("https://example.test/api?existing=1&id%5B%5D=42");
    }

    [Fact]
    public void Build_WhenNoQuery_ReturnsBaseUrl()
    {
        PythagorasQuery<SampleDto> query = new();

        HttpRequestMessage request = query.Build(HttpMethod.Get, "https://example.test/rest/v1/buildings");

        request.RequestUri!.ToString().ShouldBe("https://example.test/rest/v1/buildings");
    }

    [Fact]
    public void ImmutableBehavior_PreservesOriginalState()
    {
        PythagorasQuery<SampleDto> original = new PythagorasQuery<SampleDto>()
            .GeneralSearch("foo");

        string originalQuery = original.BuildAsQueryString();

        // Creating a new query with different search term returns new instance
        PythagorasQuery<SampleDto> modified = original.GeneralSearch("bar");
        string modifiedQuery = modified.BuildAsQueryString();

        // Original should be unchanged
        original.BuildAsQueryString().ShouldBe(originalQuery);
        modifiedQuery.ShouldNotBe(originalQuery);
    }

    [Fact]
    public void ImmutableBehavior_PreservesSkipTakeFlags()
    {
        PythagorasQuery<SampleDto> original = new PythagorasQuery<SampleDto>()
            .Skip(5);

        // With immutable pattern, state is preserved - attempting Page should still fail
        InvalidOperationException ex = Should.Throw<InvalidOperationException>(() => original.Page(1, 10));
        ex.Message.ShouldContain("Page()");
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
