using Umea.se.EstateService.Logic.Data;
using Umea.se.EstateService.Logic.Search.Providers;
using Umea.se.EstateService.Shared.Data.Entities;
using Umea.se.EstateService.Shared.Search;
using Umea.se.EstateService.Test.TestHelpers;

namespace Umea.se.EstateService.Test.Search;

public class DataStoreDocumentProviderTests
{
    [Fact]
    public async Task GetDocumentsAsync_EstateWithBuildings_NumChildrenMatchesBuildingCount()
    {
        BuildingEntity b1 = new() { Id = 10, Name = "B1", EstateId = 1 };
        BuildingEntity b2 = new() { Id = 11, Name = "B2", EstateId = 1 };

        EstateEntity estate = new()
        {
            Id = 1,
            Name = "Gamla Kyrkskolan",
            Buildings = [b1, b2]
        };

        InMemoryDataStore dataStore = new();
        DataStoreSeeder.Seed(dataStore, estates: [estate], buildings: [b1, b2]);

        DataStoreDocumentProvider provider = new(dataStore);

        ICollection<PythagorasDocument> docs = await provider.GetDocumentsAsync();

        PythagorasDocument estateDoc = docs.Single(d => d.Type == NodeType.Estate && d.Id == 1);
        estateDoc.NumChildren.ShouldBe(2);
    }
}
