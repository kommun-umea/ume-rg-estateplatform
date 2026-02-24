using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Umea.se.EstateService.DataStore.Entities;
using Umea.se.EstateService.Shared.Data;
using Umea.se.EstateService.Shared.Data.Entities;
using Umea.se.EstateService.Shared.Models;
using Umea.se.EstateService.Shared.ValueObjects;

namespace Umea.se.EstateService.DataStore.SqlServer;

/// <summary>
/// SQL Server persistence implementation for the data store.
/// Implements IDataStorePersistence to save and load snapshots from SQL Server.
/// Uses SqlBulkCopy for high-performance bulk inserts.
/// </summary>
public sealed class SqlServerPersistence(
    IDbContextFactory<EstateDbContext> dbContextFactory,
    ILogger<SqlServerPersistence> logger) : IDataStorePersistence
{
    private readonly IDbContextFactory<EstateDbContext> _dbContextFactory = dbContextFactory;
    private readonly ILogger<SqlServerPersistence> _logger = logger;

    public async Task<(DataSnapshot? Snapshot, DateTimeOffset? LastRefresh)> TryLoadAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Loading data from SQL Server database...");

            await using EstateDbContext context = await _dbContextFactory.CreateDbContextAsync(ct);

            // Check sync metadata first
            DataSyncMetadata? metadata = await context.SyncMetadata
                .AsNoTracking()
                .OrderBy(m => m.Id)
                .FirstOrDefaultAsync(ct);

            if (metadata is null)
            {
                _logger.LogInformation("No sync metadata found in database");
                return (null, null);
            }

            // Check if database has data
            if (!await context.Estates.AnyAsync(ct))
            {
                _logger.LogInformation("Database is empty, no data to load");
                return (null, null);
            }

            // Load all entities
            List<EstateEntity> estates = await context.Estates
                .AsNoTracking()
                .ToListAsync(ct);

            List<BuildingEntity> buildings = await context.Buildings
                .AsNoTracking()
                .ToListAsync(ct);

            List<FloorEntity> floors = await context.Floors
                .AsNoTracking()
                .ToListAsync(ct);

            List<RoomEntity> rooms = await context.Rooms
                .AsNoTracking()
                .ToListAsync(ct);

            List<BuildingAscendantDbEntity> ascendantEntities = await context.BuildingAscendants
                .AsNoTracking()
                .ToListAsync(ct);

            // Build relationships
            BuildEntityRelationships(estates, buildings, floors, rooms);

            // Convert ascendants to triplets
            Dictionary<int, BuildingAscendantTriplet> buildingAscendants = ascendantEntities
                .ToDictionary(a => a.BuildingId, ToDomainTriplet);

            // Create snapshot
            DataSnapshot snapshot = new(
                estates: [.. estates],
                buildings: [.. buildings],
                floors: [.. floors],
                rooms: [.. rooms],
                buildingAscendants: buildingAscendants,
                refreshUtc: metadata.LastRefreshUtc
            );

            _logger.LogInformation(
                "Loaded from database: {EstateCount} estates, {BuildingCount} buildings, {FloorCount} floors, {RoomCount} rooms",
                estates.Count, buildings.Count, floors.Count, rooms.Count);

            return (snapshot, metadata.LastRefreshUtc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load data from database");
            return (null, null);
        }
    }

    public async Task SaveAsync(DataSnapshot snapshot, DateTimeOffset refreshTime, CancellationToken ct = default)
    {
        if (!snapshot.IsReady)
        {
            return;
        }

        _logger.LogInformation("Persisting data to SQL Server database...");

        // Use a throwaway context to get the execution strategy — the real context is created inside the lambda
        await using EstateDbContext strategyContext = await _dbContextFactory.CreateDbContextAsync(ct);
        IExecutionStrategy strategy = strategyContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            // Fresh context per attempt so state is clean on retries
            await using EstateDbContext context = await _dbContextFactory.CreateDbContextAsync(ct);

            SqlConnection sqlConnection = (SqlConnection)context.Database.GetDbConnection();
            await sqlConnection.OpenAsync(ct);

            await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(ct);
            SqlTransaction sqlTransaction = (SqlTransaction)transaction.GetDbTransaction();

            // Clear existing data (FK-safe order)
            await context.Database.ExecuteSqlRawAsync("DELETE FROM [BuildingAscendants]", ct);
            await context.Database.ExecuteSqlRawAsync("DELETE FROM [Rooms]", ct);
            await context.Database.ExecuteSqlRawAsync("DELETE FROM [Floors]", ct);
            await context.Database.ExecuteSqlRawAsync("DELETE FROM [Buildings]", ct);
            await context.Database.ExecuteSqlRawAsync("DELETE FROM [Estates]", ct);
            await context.Database.ExecuteSqlRawAsync("DELETE FROM [SyncMetadata]", ct);

            // Bulk insert all tables
            await BulkInsertAsync(sqlConnection, sqlTransaction, "Estates", BuildEstatesTable(snapshot.Estates), ct);
            await BulkInsertAsync(sqlConnection, sqlTransaction, "Buildings", BuildBuildingsTable(snapshot.Buildings), ct);
            await BulkInsertAsync(sqlConnection, sqlTransaction, "Floors", BuildFloorsTable(snapshot.Floors), ct);
            await BulkInsertAsync(sqlConnection, sqlTransaction, "Rooms", BuildRoomsTable(snapshot.Rooms), ct);
            await BulkInsertAsync(sqlConnection, sqlTransaction, "BuildingAscendants",
                BuildAscendantsTable(snapshot.BuildingAscendants), ct);
            await BulkInsertAsync(sqlConnection, sqlTransaction, "SyncMetadata",
                BuildSyncMetadataTable(refreshTime, snapshot), ct);

            await transaction.CommitAsync(ct);

            _logger.LogInformation(
                "Persisted to database: {EstateCount} estates, {BuildingCount} buildings, {FloorCount} floors, {RoomCount} rooms",
                snapshot.Estates.Length, snapshot.Buildings.Length, snapshot.Floors.Length, snapshot.Rooms.Length);
        });
    }

    private static async Task BulkInsertAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string tableName,
        DataTable table,
        CancellationToken ct)
    {
        if (table.Rows.Count == 0)
        {
            return;
        }

        using SqlBulkCopy bulkCopy = new(connection, SqlBulkCopyOptions.Default, transaction);
        bulkCopy.DestinationTableName = tableName;
        bulkCopy.BulkCopyTimeout = 120;

        // Map DataTable columns to SQL columns by name
        foreach (DataColumn column in table.Columns)
        {
            bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
        }

        await bulkCopy.WriteToServerAsync(table, ct);
    }

    #region DataTable Builders

    private static DataTable BuildEstatesTable(IReadOnlyList<EstateEntity> estates)
    {
        DataTable table = new();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("Uid", typeof(Guid));
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("PopularName", typeof(string));
        table.Columns.Add("UpdatedAt", typeof(DateTimeOffset));
        table.Columns.Add("GrossArea", typeof(decimal));
        table.Columns.Add("NetArea", typeof(decimal));
        table.Columns.Add("GeoLocationLat", typeof(double));
        table.Columns.Add("GeoLocationLon", typeof(double));
        table.Columns.Add("AddressStreet", typeof(string));
        table.Columns.Add("AddressZipCode", typeof(string));
        table.Columns.Add("AddressCity", typeof(string));
        table.Columns.Add("AddressCountry", typeof(string));
        table.Columns.Add("AddressExtra", typeof(string));
        table.Columns.Add("PropertyDesignation", typeof(string));
        table.Columns.Add("OperationalArea", typeof(string));
        table.Columns.Add("AdministrativeArea", typeof(string));
        table.Columns.Add("MunicipalityArea", typeof(string));
        table.Columns.Add("ExternalOwnerStatus", typeof(string));
        table.Columns.Add("ExternalOwnerName", typeof(string));
        table.Columns.Add("ExternalOwnerNote", typeof(string));
        table.Columns.Add("BuildingCount", typeof(int));

        foreach (EstateEntity e in estates)
        {
            table.Rows.Add(
                e.Id,
                e.Uid,
                e.Name,
                e.PopularName,
                e.UpdatedAt,
                e.GrossArea,
                e.NetArea,
                (object?)e.GeoLocation?.Lat ?? DBNull.Value,
                (object?)e.GeoLocation?.Lon ?? DBNull.Value,
                (object?)e.Address?.Street ?? DBNull.Value,
                (object?)e.Address?.ZipCode ?? DBNull.Value,
                (object?)e.Address?.City ?? DBNull.Value,
                (object?)e.Address?.Country ?? DBNull.Value,
                (object?)e.Address?.Extra ?? DBNull.Value,
                (object?)e.PropertyDesignation ?? DBNull.Value,
                (object?)e.OperationalArea ?? DBNull.Value,
                (object?)e.AdministrativeArea ?? DBNull.Value,
                (object?)e.MunicipalityArea ?? DBNull.Value,
                (object?)e.ExternalOwnerStatus ?? DBNull.Value,
                (object?)e.ExternalOwnerName ?? DBNull.Value,
                (object?)e.ExternalOwnerNote ?? DBNull.Value,
                e.BuildingCount
            );
        }

        return table;
    }

    private static DataTable BuildBuildingsTable(IReadOnlyList<BuildingEntity> buildings)
    {
        DataTable table = new();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("Uid", typeof(Guid));
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("PopularName", typeof(string));
        table.Columns.Add("UpdatedAt", typeof(DateTimeOffset));
        table.Columns.Add("GrossArea", typeof(decimal));
        table.Columns.Add("NetArea", typeof(decimal));
        table.Columns.Add("EstateId", typeof(int));
        table.Columns.Add("BusinessTypeId", typeof(int));
        table.Columns.Add("BusinessTypeName", typeof(string));
        table.Columns.Add("NumFloors", typeof(int));
        table.Columns.Add("NumRooms", typeof(int));
        table.Columns.Add("GeoLocationLat", typeof(double));
        table.Columns.Add("GeoLocationLon", typeof(double));
        table.Columns.Add("AddressStreet", typeof(string));
        table.Columns.Add("AddressZipCode", typeof(string));
        table.Columns.Add("AddressCity", typeof(string));
        table.Columns.Add("AddressCountry", typeof(string));
        table.Columns.Add("AddressExtra", typeof(string));
        table.Columns.Add("YearOfConstruction", typeof(string));
        table.Columns.Add("BuildingCondition", typeof(string));
        table.Columns.Add("ExternalOwnerStatus", typeof(string));
        table.Columns.Add("ExternalOwnerName", typeof(string));
        table.Columns.Add("ExternalOwnerNote", typeof(string));
        table.Columns.Add("PropertyDesignation", typeof(string));
        table.Columns.Add("NoticeBoardText", typeof(string));
        table.Columns.Add("NoticeBoardStartDate", typeof(DateTime));
        table.Columns.Add("NoticeBoardEndDate", typeof(DateTime));
        table.Columns.Add("BlueprintAvailable", typeof(bool));
        table.Columns.Add("PropertyManager", typeof(string));
        table.Columns.Add("OperationsManager", typeof(string));
        table.Columns.Add("OperationCoordinator", typeof(string));
        table.Columns.Add("RentalAdministrator", typeof(string));
        table.Columns.Add("ImageIds", typeof(string));

        foreach (BuildingEntity b in buildings)
        {
            table.Rows.Add(
                b.Id,
                b.Uid,
                b.Name,
                b.PopularName,
                b.UpdatedAt,
                b.GrossArea,
                b.NetArea,
                b.EstateId,
                (object?)b.BusinessType?.Id ?? DBNull.Value,
                (object?)b.BusinessType?.Name ?? DBNull.Value,
                b.NumFloors,
                b.NumRooms,
                (object?)b.GeoLocation?.Lat ?? DBNull.Value,
                (object?)b.GeoLocation?.Lon ?? DBNull.Value,
                (object?)b.Address?.Street ?? DBNull.Value,
                (object?)b.Address?.ZipCode ?? DBNull.Value,
                (object?)b.Address?.City ?? DBNull.Value,
                (object?)b.Address?.Country ?? DBNull.Value,
                (object?)b.Address?.Extra ?? DBNull.Value,
                (object?)b.YearOfConstruction ?? DBNull.Value,
                (object?)b.BuildingCondition ?? DBNull.Value,
                (object?)b.ExternalOwnerStatus ?? DBNull.Value,
                (object?)b.ExternalOwnerName ?? DBNull.Value,
                (object?)b.ExternalOwnerNote ?? DBNull.Value,
                (object?)b.PropertyDesignation ?? DBNull.Value,
                (object?)b.NoticeBoard?.Text ?? DBNull.Value,
                (object?)b.NoticeBoard?.StartDate ?? DBNull.Value,
                (object?)b.NoticeBoard?.EndDate ?? DBNull.Value,
                (object?)b.BlueprintAvailable ?? DBNull.Value,
                (object?)b.ContactPersons?.PropertyManager ?? DBNull.Value,
                (object?)b.ContactPersons?.OperationsManager ?? DBNull.Value,
                (object?)b.ContactPersons?.OperationCoordinator ?? DBNull.Value,
                (object?)b.ContactPersons?.RentalAdministrator ?? DBNull.Value,
                b.ImageIds is { Count: > 0 }
                    ? JsonSerializer.Serialize(b.ImageIds, (JsonSerializerOptions?)null)
                    : DBNull.Value
            );
        }

        return table;
    }

    private static DataTable BuildFloorsTable(IReadOnlyList<FloorEntity> floors)
    {
        DataTable table = new();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("Uid", typeof(Guid));
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("PopularName", typeof(string));
        table.Columns.Add("UpdatedAt", typeof(DateTimeOffset));
        table.Columns.Add("GrossArea", typeof(double));
        table.Columns.Add("NetArea", typeof(double));
        table.Columns.Add("Height", typeof(double));
        table.Columns.Add("BuildingId", typeof(int));

        foreach (FloorEntity f in floors)
        {
            table.Rows.Add(
                f.Id,
                f.Uid,
                f.Name,
                f.PopularName,
                f.UpdatedAt,
                (object?)f.GrossArea ?? DBNull.Value,
                (object?)f.NetArea ?? DBNull.Value,
                (object?)f.Height ?? DBNull.Value,
                f.BuildingId
            );
        }

        return table;
    }

    private static DataTable BuildRoomsTable(IReadOnlyList<RoomEntity> rooms)
    {
        DataTable table = new();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("Uid", typeof(Guid));
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("PopularName", typeof(string));
        table.Columns.Add("UpdatedAt", typeof(DateTimeOffset));
        table.Columns.Add("GrossArea", typeof(double));
        table.Columns.Add("NetArea", typeof(double));
        table.Columns.Add("Capacity", typeof(int));
        table.Columns.Add("BuildingId", typeof(int));
        table.Columns.Add("FloorId", typeof(int));

        foreach (RoomEntity r in rooms)
        {
            table.Rows.Add(
                r.Id,
                r.Uid,
                r.Name,
                r.PopularName,
                r.UpdatedAt,
                r.GrossArea,
                r.NetArea,
                r.Capacity,
                r.BuildingId,
                (object?)r.FloorId ?? DBNull.Value
            );
        }

        return table;
    }

    private static DataTable BuildAscendantsTable(IReadOnlyDictionary<int, BuildingAscendantTriplet> ascendants)
    {
        DataTable table = new();
        table.Columns.Add("BuildingId", typeof(int));
        table.Columns.Add("EstateAscendantId", typeof(int));
        table.Columns.Add("EstateAscendantName", typeof(string));
        table.Columns.Add("EstateAscendantPopularName", typeof(string));
        table.Columns.Add("EstateAscendantGeoLat", typeof(double));
        table.Columns.Add("EstateAscendantGeoLon", typeof(double));
        table.Columns.Add("RegionAscendantId", typeof(int));
        table.Columns.Add("RegionAscendantName", typeof(string));
        table.Columns.Add("RegionAscendantPopularName", typeof(string));
        table.Columns.Add("RegionAscendantGeoLat", typeof(double));
        table.Columns.Add("RegionAscendantGeoLon", typeof(double));
        table.Columns.Add("OrganizationAscendantId", typeof(int));
        table.Columns.Add("OrganizationAscendantName", typeof(string));
        table.Columns.Add("OrganizationAscendantPopularName", typeof(string));
        table.Columns.Add("OrganizationAscendantGeoLat", typeof(double));
        table.Columns.Add("OrganizationAscendantGeoLon", typeof(double));

        foreach ((int buildingId, BuildingAscendantTriplet triplet) in ascendants)
        {
            table.Rows.Add(
                buildingId,
                (object?)triplet.Estate?.Id ?? DBNull.Value,
                (object?)triplet.Estate?.Name ?? DBNull.Value,
                (object?)triplet.Estate?.PopularName ?? DBNull.Value,
                (object?)triplet.Estate?.GeoLocation?.Lat ?? DBNull.Value,
                (object?)triplet.Estate?.GeoLocation?.Lon ?? DBNull.Value,
                (object?)triplet.Region?.Id ?? DBNull.Value,
                (object?)triplet.Region?.Name ?? DBNull.Value,
                (object?)triplet.Region?.PopularName ?? DBNull.Value,
                (object?)triplet.Region?.GeoLocation?.Lat ?? DBNull.Value,
                (object?)triplet.Region?.GeoLocation?.Lon ?? DBNull.Value,
                (object?)triplet.Organization?.Id ?? DBNull.Value,
                (object?)triplet.Organization?.Name ?? DBNull.Value,
                (object?)triplet.Organization?.PopularName ?? DBNull.Value,
                (object?)triplet.Organization?.GeoLocation?.Lat ?? DBNull.Value,
                (object?)triplet.Organization?.GeoLocation?.Lon ?? DBNull.Value
            );
        }

        return table;
    }

    private static DataTable BuildSyncMetadataTable(DateTimeOffset refreshTime, DataSnapshot snapshot)
    {
        DataTable table = new();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("LastRefreshUtc", typeof(DateTimeOffset));
        table.Columns.Add("EstateCount", typeof(int));
        table.Columns.Add("BuildingCount", typeof(int));
        table.Columns.Add("FloorCount", typeof(int));
        table.Columns.Add("RoomCount", typeof(int));

        table.Rows.Add(1, refreshTime, snapshot.Estates.Length, snapshot.Buildings.Length,
            snapshot.Floors.Length, snapshot.Rooms.Length);

        return table;
    }

    #endregion

    #region Entity Relationships

    private static void BuildEntityRelationships(
        List<EstateEntity> estates,
        List<BuildingEntity> buildings,
        List<FloorEntity> floors,
        List<RoomEntity> rooms)
    {
        Dictionary<int, EstateEntity> estatesById = estates.ToDictionary(e => e.Id);
        Dictionary<int, BuildingEntity> buildingsById = buildings.ToDictionary(b => b.Id);
        Dictionary<int, FloorEntity> floorsById = floors.ToDictionary(f => f.Id);

        // Link buildings to estates
        foreach (BuildingEntity building in buildings)
        {
            if (building.EstateId > 0 && estatesById.TryGetValue(building.EstateId, out EstateEntity? estate))
            {
                estate.Buildings.Add(building);
            }
        }

        // Link floors to buildings
        foreach (FloorEntity floor in floors)
        {
            if (floor.BuildingId > 0 && buildingsById.TryGetValue(floor.BuildingId, out BuildingEntity? building))
            {
                building.Floors.Add(floor);
            }
        }

        // Link rooms to buildings and floors
        foreach (RoomEntity room in rooms)
        {
            if (room.BuildingId > 0 && buildingsById.TryGetValue(room.BuildingId, out BuildingEntity? building))
            {
                building.Rooms.Add(room);
            }

            if (room.FloorId.HasValue && floorsById.TryGetValue(room.FloorId.Value, out FloorEntity? floor))
            {
                floor.Rooms.Add(room);
            }
        }

        // Update counts
        foreach (EstateEntity estate in estates)
        {
            estate.BuildingCount = estate.Buildings.Count;
        }

        foreach (BuildingEntity building in buildings)
        {
            building.NumFloors = building.Floors.Count;
            building.NumRooms = building.Rooms.Count;
        }
    }

    #endregion

    #region Ascendant Mapping

    private static BuildingAscendantTriplet ToDomainTriplet(BuildingAscendantDbEntity entity)
    {
        BuildingAscendantModel? estate = entity.EstateAscendantId.HasValue
            ? new BuildingAscendantModel
            {
                Id = entity.EstateAscendantId.Value,
                Name = entity.EstateAscendantName ?? string.Empty,
                PopularName = entity.EstateAscendantPopularName,
                GeoLocation = entity.EstateAscendantGeoLat.HasValue && entity.EstateAscendantGeoLon.HasValue
                    ? new GeoPointModel(entity.EstateAscendantGeoLat.Value, entity.EstateAscendantGeoLon.Value)
                    : null,
                Type = BuildingAscendantType.Estate
            }
            : null;

        BuildingAscendantModel? region = entity.RegionAscendantId.HasValue
            ? new BuildingAscendantModel
            {
                Id = entity.RegionAscendantId.Value,
                Name = entity.RegionAscendantName ?? string.Empty,
                PopularName = entity.RegionAscendantPopularName,
                GeoLocation = entity.RegionAscendantGeoLat.HasValue && entity.RegionAscendantGeoLon.HasValue
                    ? new GeoPointModel(entity.RegionAscendantGeoLat.Value, entity.RegionAscendantGeoLon.Value)
                    : null,
                Type = BuildingAscendantType.Area
            }
            : null;

        BuildingAscendantModel? organization = entity.OrganizationAscendantId.HasValue
            ? new BuildingAscendantModel
            {
                Id = entity.OrganizationAscendantId.Value,
                Name = entity.OrganizationAscendantName ?? string.Empty,
                PopularName = entity.OrganizationAscendantPopularName,
                GeoLocation = entity.OrganizationAscendantGeoLat.HasValue && entity.OrganizationAscendantGeoLon.HasValue
                    ? new GeoPointModel(entity.OrganizationAscendantGeoLat.Value, entity.OrganizationAscendantGeoLon.Value)
                    : null,
                Type = BuildingAscendantType.Organization
            }
            : null;

        return new BuildingAscendantTriplet
        {
            Estate = estate,
            Region = region,
            Organization = organization
        };
    }

    #endregion
}
