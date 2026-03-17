using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Umea.se.EstateService.DataStore;
using Umea.se.EstateService.DataStore.SqlServer;
using Umea.se.EstateService.Logic.Data;
using Umea.se.EstateService.Logic.Handlers.Favorite;
using Umea.se.EstateService.Shared.Data.Entities;
using Umea.se.EstateService.Shared.Search;
using Umea.se.EstateService.Test.TestHelpers;

namespace Umea.se.EstateService.Test.Handlers;

public class FavoriteHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly EstateDbContext _dbContext;
    private readonly InMemoryDataStore _dataStore;
    private readonly FavoriteHandler _handler;

    private static readonly EstateEntity TestEstate = new() { Id = 1, Name = "Estate One", PopularName = "E1" };
    private static readonly BuildingEntity TestBuilding = new() { Id = 10, Name = "Building Ten", PopularName = "B10", EstateId = 1 };
    private static readonly RoomEntity TestRoom = new() { Id = 100, Name = "Room Hundred", PopularName = "R100", BuildingId = 10 };

    public FavoriteHandlerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        DbContextOptions<EstateDbContext> options = new DbContextOptionsBuilder<EstateDbContext>()
            .UseSqlite(_connection)
            .Options;

        using (EstateDbContext context = new(options))
        {
            context.Database.EnsureCreated();
        }

        _dbContext = new EstateDbContext(options);
        FavoriteRepository favoriteRepository = new(_dbContext);
        _dataStore = new InMemoryDataStore();

        SeedDefaultData();

        _handler = new FavoriteHandler(favoriteRepository, _dataStore);
    }

    [Fact]
    public async Task SetFavorite_Building_CanBeRetrieved()
    {
        await _handler.SetFavoriteAsync("user@test.com", NodeType.Building, 10);

        IReadOnlyList<PythagorasDocument> result = await _handler.GetFavoritesAsync("user@test.com");

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(10);
        result[0].Type.ShouldBe(NodeType.Building);
        result[0].Name.ShouldBe("Building Ten");
    }

    [Fact]
    public async Task SetFavorite_Duplicate_IsIdempotent()
    {
        await _handler.SetFavoriteAsync("user@test.com", NodeType.Building, 10);
        await _handler.SetFavoriteAsync("user@test.com", NodeType.Building, 10);

        IReadOnlyList<PythagorasDocument> result = await _handler.GetFavoritesAsync("user@test.com");

        result.Count.ShouldBe(1);
    }

    [Fact]
    public async Task RemoveFavorite_Existing_RemovesIt()
    {
        await _handler.SetFavoriteAsync("user@test.com", NodeType.Building, 10);
        await _handler.RemoveFavoriteAsync("user@test.com", NodeType.Building, 10);

        IReadOnlyList<PythagorasDocument> result = await _handler.GetFavoritesAsync("user@test.com");

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task RemoveFavorite_NonExistent_DoesNotThrow()
    {
        await _handler.RemoveFavoriteAsync("user@test.com", NodeType.Building, 9999);
    }

    [Fact]
    public async Task GetFavorites_Building_HasEstateAncestor()
    {
        await _handler.SetFavoriteAsync("user@test.com", NodeType.Building, 10);

        IReadOnlyList<PythagorasDocument> result = await _handler.GetFavoritesAsync("user@test.com");

        result.Count.ShouldBe(1);
        result[0].Ancestors.Count.ShouldBe(1);
        result[0].Ancestors[0].Id.ShouldBe(1);
        result[0].Ancestors[0].Type.ShouldBe(NodeType.Estate);
        result[0].Ancestors[0].Name.ShouldBe("Estate One");
    }

    [Fact]
    public async Task GetFavorites_Estate_ReturnsDocument()
    {
        await _handler.SetFavoriteAsync("user@test.com", NodeType.Estate, 1);

        IReadOnlyList<PythagorasDocument> result = await _handler.GetFavoritesAsync("user@test.com");

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(1);
        result[0].Type.ShouldBe(NodeType.Estate);
        result[0].Name.ShouldBe("Estate One");
        result[0].Ancestors.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetFavorites_Room_HasBuildingAndEstateAncestors()
    {
        await _handler.SetFavoriteAsync("user@test.com", NodeType.Room, 100);

        IReadOnlyList<PythagorasDocument> result = await _handler.GetFavoritesAsync("user@test.com");

        result.Count.ShouldBe(1);
        result[0].Ancestors.Count.ShouldBe(2);
        result[0].Ancestors[0].Type.ShouldBe(NodeType.Estate);
        result[0].Ancestors[0].Id.ShouldBe(1);
        result[0].Ancestors[1].Type.ShouldBe(NodeType.Building);
        result[0].Ancestors[1].Id.ShouldBe(10);
    }

    [Fact]
    public async Task GetFavorites_NodeNoLongerInDataStore_SkipsIt()
    {
        await _handler.SetFavoriteAsync("user@test.com", NodeType.Building, 10);

        // Re-seed without the building
        DataStoreSeeder.Seed(_dataStore, estates: [TestEstate]);

        IReadOnlyList<PythagorasDocument> result = await _handler.GetFavoritesAsync("user@test.com");

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetFavorites_ReturnsOnlyCurrentUserFavorites()
    {
        await _handler.SetFavoriteAsync("usera@test.com", NodeType.Building, 10);
        await _handler.SetFavoriteAsync("userb@test.com", NodeType.Estate, 1);

        IReadOnlyList<PythagorasDocument> resultA = await _handler.GetFavoritesAsync("usera@test.com");
        IReadOnlyList<PythagorasDocument> resultB = await _handler.GetFavoritesAsync("userb@test.com");

        resultA.Count.ShouldBe(1);
        resultA[0].Type.ShouldBe(NodeType.Building);

        resultB.Count.ShouldBe(1);
        resultB[0].Type.ShouldBe(NodeType.Estate);
    }

    [Fact]
    public async Task GetFavorites_Documents_HaveIsFavoriteTrue()
    {
        await _handler.SetFavoriteAsync("user@test.com", NodeType.Estate, 1);
        await _handler.SetFavoriteAsync("user@test.com", NodeType.Building, 10);

        IReadOnlyList<PythagorasDocument> result = await _handler.GetFavoritesAsync("user@test.com");

        result.Count.ShouldBe(2);
        result.ShouldAllBe(d => d.IsFavorite == true);
    }

    [Fact]
    public async Task GetFavoriteIds_ReturnsCorrectSet()
    {
        await _handler.SetFavoriteAsync("user@test.com", NodeType.Estate, 1);
        await _handler.SetFavoriteAsync("user@test.com", NodeType.Building, 10);
        await _handler.SetFavoriteAsync("user@test.com", NodeType.Room, 100);

        HashSet<(NodeType Type, int Id)> ids = await _handler.GetFavoriteIdsAsync("user@test.com");

        ids.Count.ShouldBe(3);
        ids.ShouldContain((NodeType.Estate, 1));
        ids.ShouldContain((NodeType.Building, 10));
        ids.ShouldContain((NodeType.Room, 100));
    }

    [Fact]
    public async Task GetFavoriteIds_DifferentUser_ReturnsEmpty()
    {
        await _handler.SetFavoriteAsync("user@test.com", NodeType.Building, 10);

        HashSet<(NodeType Type, int Id)> ids = await _handler.GetFavoriteIdsAsync("other@test.com");

        ids.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetFavorites_MixedTypes_ReturnsAll()
    {
        await _handler.SetFavoriteAsync("user@test.com", NodeType.Estate, 1);
        await _handler.SetFavoriteAsync("user@test.com", NodeType.Building, 10);
        await _handler.SetFavoriteAsync("user@test.com", NodeType.Room, 100);

        IReadOnlyList<PythagorasDocument> result = await _handler.GetFavoritesAsync("user@test.com");

        result.Count.ShouldBe(3);
        result.Select(d => d.Type).ShouldBe(
            [NodeType.Room, NodeType.Building, NodeType.Estate],
            ignoreOrder: true);
    }

    private void SeedDefaultData()
    {
        DataStoreSeeder.Seed(
            _dataStore,
            estates: [TestEstate],
            buildings: [TestBuilding],
            rooms: [TestRoom]);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}
