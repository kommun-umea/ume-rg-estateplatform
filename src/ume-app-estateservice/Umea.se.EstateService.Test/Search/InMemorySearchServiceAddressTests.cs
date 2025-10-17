using Umea.se.EstateService.Logic.Search;
using Umea.se.EstateService.Shared.Search;

namespace Umea.se.EstateService.Test.Search;

public class InMemorySearchServiceAddressTests
{
    [Fact]
    public void Search_FindsDocumentsByAddressTokens()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        PythagorasDocument building = new()
        {
            Id = 1,
            Type = NodeType.Building,
            Name = "Library",
            PopularName = "Central Library",
            Address = "Skolgatan 31A 901 84 Umeå",
            Ancestors = [],
            UpdatedAt = now,
            RankScore = 1
        };

        PythagorasDocument room = new()
        {
            Id = 2,
            Type = NodeType.Room,
            Name = "Conference Room",
            PopularName = "Conf A",
            Address = "Skolgatan 31A 901 84 Umeå",
            Ancestors =
            [
                new Ancestor
                {
                    Id = building.Id,
                    Type = NodeType.Building,
                    Name = building.Name,
                    PopularName = building.PopularName
                }
            ],
            UpdatedAt = now,
            RankScore = 2
        };

        PythagorasDocument other = new()
        {
            Id = 3,
            Type = NodeType.Building,
            Name = "Annex",
            PopularName = "Annex",
            Address = null,
            Ancestors = [],
            UpdatedAt = now,
            RankScore = 3
        };

        InMemorySearchService service = new([building, room, other]);

        List<SearchResult> results = [.. service.Search("Skolgatan 31A", new QueryOptions(MaxResults: 5))];

        results.Count.ShouldBeGreaterThanOrEqualTo(2);
        results.ShouldContain(r => r.Item.Id == building.Id);
        results.ShouldContain(r => r.Item.Id == room.Id);
        results.ShouldNotContain(r => r.Item.Id == other.Id);
    }

    [Fact]
    public void Search_PrioritizesPopularNamePrefixOverSuppressedEntries()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        PythagorasDocument suppressed = new()
        {
            Id = 1,
            Type = NodeType.Building,
            Name = "458-06",
            PopularName = "(Ta bort)STADSHUSET RYTTMÄSTAREN",
            Address = null,
            Ancestors = [],
            UpdatedAt = now,
            RankScore = 1
        };

        PythagorasDocument desired = new()
        {
            Id = 2,
            Type = NodeType.Building,
            Name = "458-01, -09, -18, -19",
            PopularName = "Stadshuset norra flygeln, kuben, länken",
            Address = "SKOLGATAN 31, 903 25 UMEÅ",
            Ancestors = [],
            UpdatedAt = now,
            RankScore = 2
        };

        InMemorySearchService service = new([suppressed, desired]);

        List<SearchResult> results = [.. service.Search("stadshus", new QueryOptions(MaxResults: 5))];

        results.Count.ShouldBeGreaterThanOrEqualTo(2);
        results[0].Item.Id.ShouldBe(desired.Id);
    }
}
