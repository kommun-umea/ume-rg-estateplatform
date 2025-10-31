using System.Threading;
using Microsoft.Extensions.Options;
using Umea.se.EstateService.Logic.Handlers;
using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.Logic.Options;
using Umea.se.EstateService.Logic.Search;
using Umea.se.EstateService.Shared.Autocomplete;
using Umea.se.EstateService.Shared.Search;

namespace Umea.se.EstateService.Test.Search;

public class SearchHandlerIndexingTests
{
    [Fact]
    public async Task SearchAsync_WhenRoomsExcluded_DoesNotReturnRoomDocuments()
    {
        List<PythagorasDocument> documents = CreateDocuments();
        SearchHandler handler = CreateHandler(documents, excludeRooms: true);

        await handler.RefreshIndexAsync();

        handler.GetDocumentCount().ShouldBe(2);

        IReadOnlyList<SearchResult> buildingResults = await handler.SearchAsync("Building", Array.Empty<AutocompleteType>(), 10);
        buildingResults.ShouldContain(result => result.Item.Type == NodeType.Building);

        IReadOnlyList<SearchResult> roomResults = await handler.SearchAsync("Room", Array.Empty<AutocompleteType>(), 10);
        roomResults.ShouldBeEmpty();
    }

    [Fact]
    public async Task SearchAsync_WhenRoomsIncluded_ReturnsRoomDocuments()
    {
        List<PythagorasDocument> documents = CreateDocuments();
        SearchHandler handler = CreateHandler(documents, excludeRooms: false);

        await handler.RefreshIndexAsync();

        handler.GetDocumentCount().ShouldBe(3);

        IReadOnlyList<SearchResult> roomResults = await handler.SearchAsync("Room", Array.Empty<AutocompleteType>(), 10);
        roomResults.ShouldContain(result => result.Item.Type == NodeType.Room);
    }

    [Fact]
    public async Task GetIndexedDocumentsAsync_ReturnsSnapshotWithAllDocuments()
    {
        List<PythagorasDocument> documents = CreateDocuments();
        SearchHandler handler = CreateHandler(documents, excludeRooms: true);

        IReadOnlyCollection<PythagorasDocument> indexed = await handler.GetIndexedDocumentsAsync();

        indexed.Count.ShouldBe(documents.Count);
        indexed.ShouldContain(doc => doc.Type == NodeType.Room);
    }

    [Fact]
    public async Task GetBuildingDocumentsByIdsAsync_ReturnsRequestedBuildings()
    {
        List<PythagorasDocument> documents = CreateDocuments();
        SearchHandler handler = CreateHandler(documents, excludeRooms: true);

        IReadOnlyDictionary<int, PythagorasDocument> result = await handler.GetBuildingDocumentsByIdsAsync([2], CancellationToken.None);

        result.Count.ShouldBe(1);
        result.Keys.ShouldContain(2);
        result[2].NumFloors.ShouldBe(2);
    }

    [Fact]
    public async Task GetBuildingsForEstateAsync_FiltersByEstateId()
    {
        List<PythagorasDocument> documents = CreateDocuments();
        SearchHandler handler = CreateHandler(documents, excludeRooms: true);

        IReadOnlyList<PythagorasDocument> result = await handler.GetBuildingsForEstateAsync(1, CancellationToken.None);

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(2);
    }

    private static SearchHandler CreateHandler(ICollection<PythagorasDocument> documents, bool excludeRooms)
    {
        IPythagorasDocumentProvider provider = new FakeDocumentProvider(documents);
        SearchOptions options = new() { ExcludeRooms = excludeRooms };
        return new SearchHandler(provider, Options.Create(options));
    }

    private static List<PythagorasDocument> CreateDocuments()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        PythagorasDocument estate = new()
        {
            Id = 1,
            Type = NodeType.Estate,
            Name = "Estate Alpha",
            PopularName = "Estate Alpha",
            RankScore = 1,
            UpdatedAt = now,
            Ancestors = []
        };

        Ancestor estateAncestor = new()
        {
            Id = estate.Id,
            Type = estate.Type,
            Name = estate.Name,
            PopularName = estate.PopularName
        };

        PythagorasDocument building = new()
        {
            Id = 2,
            Type = NodeType.Building,
            Name = "Main Building",
            PopularName = "Main Building",
            RankScore = 2,
            UpdatedAt = now,
            NumRooms = 1,
            NumFloors = 2,
            Ancestors = [estateAncestor]
        };

        estate.NumChildren = 1;

        Ancestor buildingAncestor = new()
        {
            Id = building.Id,
            Type = building.Type,
            Name = building.Name,
            PopularName = building.PopularName
        };

        PythagorasDocument room = new()
        {
            Id = 3,
            Type = NodeType.Room,
            Name = "Conference Room",
            PopularName = "Conference Room",
            RankScore = 3,
            UpdatedAt = now,
            Ancestors = [estateAncestor, buildingAncestor]
        };

        building.NumChildren = 1;

        return [estate, building, room];
    }

    private sealed class FakeDocumentProvider(ICollection<PythagorasDocument> documents) : IPythagorasDocumentProvider
    {
        public Task<ICollection<PythagorasDocument>> GetDocumentsAsync()
            => Task.FromResult(documents);
    }
}
