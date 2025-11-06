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
            GeoLocation = new GeoPoint { Lat = 63.8258, Lng = 20.2630 },
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
            GeoLocation = new GeoPoint { Lat = 63.9000, Lng = 20.5000 },
            Ancestors = []
        };

        InMemorySearchService service = new([withinRadius, outsideRadius]);

        QueryOptions options = new(
            MaxResults: 10,
            GeoFilter: new GeoRadiusFilter(
                new GeoCoordinate(withinRadius.GeoLocation!.Lat, withinRadius.GeoLocation!.Lng),
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
            GeoLocation = new GeoPoint { Lat = 63.8258, Lng = 20.2630 },
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
            GeoLocation = null,
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
            GeoLocation = new GeoPoint { Lat = 63.9000, Lng = 20.5000 },
            Ancestors = []
        };

        InMemorySearchService service = new([withGeo, withoutGeo, outsideRadius]);

        QueryOptions options = new(
            MaxResults: 10,
            GeoFilter: new GeoRadiusFilter(
                new GeoCoordinate(withGeo.GeoLocation!.Lat, withGeo.GeoLocation!.Lng),
                1_000));

        List<SearchResult> results = [.. service.Search(string.Empty, options)];

        results.ShouldContain(r => r.Item.Id == withGeo.Id);
        results.ShouldNotContain(r => r.Item.Id == withoutGeo.Id);
        results.ShouldNotContain(r => r.Item.Id == outsideRadius.Id);
    }

    [Fact]
    public void Search_WithGeoBoundingBox_FiltersDocumentsOutsideBox()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        PythagorasDocument insideBox = new()
        {
            Id = 1,
            Type = NodeType.Estate,
            Name = "Central Estate",
            PopularName = "Central Estate",
            RankScore = 1,
            UpdatedAt = now,
            GeoLocation = new GeoPoint { Lat = 63.8258, Lng = 20.2630 },
            Ancestors = []
        };

        PythagorasDocument outsideBox = new()
        {
            Id = 2,
            Type = NodeType.Building,
            Name = "Far Away Building",
            PopularName = "Far Away",
            RankScore = 1,
            UpdatedAt = now,
            GeoLocation = new GeoPoint { Lat = 64.1000, Lng = 20.7000 },
            Ancestors = []
        };

        InMemorySearchService service = new([insideBox, outsideBox]);

        QueryOptions options = new(
            MaxResults: 10,
            GeoFilter: new GeoBoundingBoxFilter(
                new GeoCoordinate(63.80, 20.20),
                new GeoCoordinate(63.90, 20.30)));

        List<SearchResult> results = [.. service.Search(string.Empty, options)];

        results.ShouldContain(r => r.Item.Id == insideBox.Id);
        results.ShouldNotContain(r => r.Item.Id == outsideBox.Id);
    }
}
