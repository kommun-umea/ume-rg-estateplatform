using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Umea.se.EstateService.DataStore.EfCore;
using Umea.se.EstateService.Shared.Data;
using Umea.se.EstateService.Shared.Data.Entities;

namespace Umea.se.EstateService.DataStore.SqlServer;

/// <summary>
/// SQL Server persistence implementation for the data store.
/// Uses SqlBulkCopy for high-performance bulk inserts.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class SqlServerPersistence(
    IDbContextFactory<EstateDbContext> dbContextFactory,
    ILogger<SqlServerPersistence> logger) : EfCorePersistenceBase(dbContextFactory, logger)
{
    public override async Task SaveAsync(DataSnapshot snapshot, DateTimeOffset refreshTime, CancellationToken ct = default)
    {
        if (!snapshot.IsReady)
        {
            return;
        }

        Logger.LogInformation("Persisting data to SQL Server database...");

        // Use a throwaway context to get the execution strategy — the real context is created inside the lambda
        await using EstateDbContext strategyContext = await DbContextFactory.CreateDbContextAsync(ct);
        IExecutionStrategy strategy = strategyContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            // Fresh context per attempt so state is clean on retries
            await using EstateDbContext context = await DbContextFactory.CreateDbContextAsync(ct);

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

            Logger.LogInformation(
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
        table.Columns.Add("WorkOrderTypes", typeof(string));
        table.Columns.Add("ImageIds", typeof(string));
        table.Columns.Add("NumDocuments", typeof(int));
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
                JsonSerializer.Serialize(b.WorkOrderTypes, (JsonSerializerOptions?)null),
                b.ImageIds is not null
                    ? JsonSerializer.Serialize(b.ImageIds, (JsonSerializerOptions?)null)
                    : DBNull.Value,
                (object?)b.NumDocuments ?? DBNull.Value
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
        table.Columns.Add("WorkOrderCategoriesJson", typeof(string));
        table.Columns.Add("PortalPublishStatusIdsJson", typeof(string));

        table.Rows.Add(1, refreshTime, snapshot.Estates.Length, snapshot.Buildings.Length,
            snapshot.Floors.Length, snapshot.Rooms.Length,
            SerializeCategories(snapshot.WorkOrderCategories),
            SerializeStatusIds(snapshot.PortalPublishStatusIds));

        return table;
    }

    #endregion
}
