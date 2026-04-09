using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Umea.se.EstateService.DataStore.EfCore;
using Umea.se.EstateService.DataStore.Entities;
using Umea.se.EstateService.Shared.Data;

namespace Umea.se.EstateService.DataStore.Sqlite;

/// <summary>
/// SQLite persistence implementation for the data store.
/// Uses EF Core AddRange for bulk inserts (no SqlBulkCopy available).
/// </summary>
public sealed class SqlitePersistence(
    IDbContextFactory<EstateDbContext> dbContextFactory,
    ILogger<SqlitePersistence> logger) : EfCorePersistenceBase(dbContextFactory, logger)
{
    public override async Task SaveAsync(DataSnapshot snapshot, DateTimeOffset refreshTime, CancellationToken ct = default)
    {
        if (!snapshot.IsReady)
        {
            return;
        }

        Logger.LogInformation("Persisting data to SQLite database...");

        await using EstateDbContext context = await DbContextFactory.CreateDbContextAsync(ct);
        await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(ct);

        // Clear tables (FK-safe order)
        await context.Database.ExecuteSqlRawAsync("DELETE FROM BuildingAscendants", ct);
        await context.Database.ExecuteSqlRawAsync("DELETE FROM Rooms", ct);
        await context.Database.ExecuteSqlRawAsync("DELETE FROM Floors", ct);
        await context.Database.ExecuteSqlRawAsync("DELETE FROM Buildings", ct);
        await context.Database.ExecuteSqlRawAsync("DELETE FROM Estates", ct);
        await context.Database.ExecuteSqlRawAsync("DELETE FROM SyncMetadata", ct);

        // Bulk insert via EF Core AddRange
        context.Estates.AddRange(snapshot.Estates);
        context.Buildings.AddRange(snapshot.Buildings);
        context.Floors.AddRange(snapshot.Floors);
        context.Rooms.AddRange(snapshot.Rooms);
        context.BuildingAscendants.AddRange(ToDbEntities(snapshot.BuildingAscendants));
        context.SyncMetadata.Add(new DataSyncMetadata
        {
            Id = 1,
            LastRefreshUtc = refreshTime,
            EstateCount = snapshot.Estates.Length,
            BuildingCount = snapshot.Buildings.Length,
            FloorCount = snapshot.Floors.Length,
            RoomCount = snapshot.Rooms.Length,
            WorkOrderCategoriesJson = SerializeCategories(snapshot.WorkOrderCategories),
            PortalPublishStatusIdsJson = SerializeStatusIds(snapshot.PortalPublishStatusIds)
        });

        await context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        Logger.LogInformation(
            "Persisted to SQLite: {EstateCount} estates, {BuildingCount} buildings, {FloorCount} floors, {RoomCount} rooms",
            snapshot.Estates.Length, snapshot.Buildings.Length, snapshot.Floors.Length, snapshot.Rooms.Length);
    }
}
