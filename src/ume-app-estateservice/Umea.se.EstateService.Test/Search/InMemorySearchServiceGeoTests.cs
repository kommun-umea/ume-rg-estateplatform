using Shouldly;
using Umea.se.EstateService.Logic.Search;
using Umea.se.EstateService.Shared.Search;

namespace Umea.se.EstateService.Test.Search;

public class InMemorySearchServiceGeoTests
{
    [Fact]
    public void Search_WithGeoFilters_FiltersDocumentsOutsideRadius()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        PythagorasDocument withinRadius = new()
        {
            Id = 1,
            Type = NodeType.Building,
            Name = "Main Library",
            PopularName = "Main Library",
            RankScore = 1,
            UpdatedAt = now,
            Geo = new GeoPoint { Lat = 63.8258, Lng = 20.2630 },
            Ancestors = []
        };

        PythagorasDocument outsideRadius = new()
        {
            Id = 2,
            Type = NodeType.Building,
            Name = "Main Library Annex",
            PopularName = "Annex",
            RankScore = 1,
            UpdatedAt = now,
            Geo = new GeoPoint { Lat = 63.9000, Lng = 20.5000 },
            Ancestors = []
        };

        InMemorySearchService service = new([withinRadius, outsideRadius]);

        QueryOptions options = new(
            MaxResults: 10,
            GeoFilter: new GeoFilter(
                new GeoCoordinate(withinRadius.Geo!.Lat, withinRadius.Geo!.Lng),
                500));

        List<SearchResult> results = [.. service.Search("Library", options)];

        results.ShouldContain(r => r.Item.Id == withinRadius.Id);
        results.ShouldNotContain(r => r.Item.Id == outsideRadius.Id);
    }

    [Fact]
    public void Search_WithGeoFiltersAndEmptyQuery_ReturnsDocumentsWithinRadius()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        PythagorasDocument withGeo = new()
        {
            Id = 1,
            Type = NodeType.Estate,
            Name = "Campus Estate",
            PopularName = "Campus Estate",
            RankScore = 1,
            UpdatedAt = now,
            Geo = new GeoPoint { Lat = 63.8258, Lng = 20.2630 },
            Ancestors = []
        };

        PythagorasDocument withoutGeo = new()
        {
            Id = 2,
            Type = NodeType.Building,
            Name = "Unknown Location Building",
            PopularName = "Unknown",
            RankScore = 1,
            UpdatedAt = now,
            Geo = null,
            Ancestors = []
        };

        PythagorasDocument outsideRadius = new()
        {
            Id = 3,
            Type = NodeType.Building,
            Name = "Remote Building",
            PopularName = "Remote",
            RankScore = 1,
            UpdatedAt = now,
            Geo = new GeoPoint { Lat = 63.9000, Lng = 20.5000 },
            Ancestors = []
        };

        InMemorySearchService service = new([withGeo, withoutGeo, outsideRadius]);

        QueryOptions options = new(
            MaxResults: 10,
            GeoFilter: new GeoFilter(
                new GeoCoordinate(withGeo.Geo!.Lat, withGeo.Geo!.Lng),
                1_000));

        List<SearchResult> results = [.. service.Search(string.Empty, options)];

        results.ShouldContain(r => r.Item.Id == withGeo.Id);
        results.ShouldNotContain(r => r.Item.Id == withoutGeo.Id);
        results.ShouldNotContain(r => r.Item.Id == outsideRadius.Id);
    }
}
