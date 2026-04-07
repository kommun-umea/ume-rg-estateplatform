using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Umea.se.EstateService.DataStore.Entities;
using Umea.se.EstateService.Shared.Data;
using Umea.se.EstateService.Shared.Data.Entities;
using Umea.se.EstateService.Shared.Models;
using Umea.se.EstateService.Shared.ValueObjects;

namespace Umea.se.EstateService.DataStore.EfCore;

/// <summary>
/// Abstract base class for EF Core persistence implementations.
/// Contains shared loading logic that works across all EF Core providers.
/// </summary>
public abstract class EfCorePersistenceBase(
    IDbContextFactory<EstateDbContext> dbContextFactory,
    ILogger logger) : IDataStorePersistence
{
    protected IDbContextFactory<EstateDbContext> DbContextFactory { get; } = dbContextFactory;
    protected ILogger Logger { get; } = logger;

    public async Task<(DataSnapshot? Snapshot, DateTimeOffset? LastRefresh)> TryLoadAsync(CancellationToken ct = default)
    {
        try
        {
            Logger.LogInformation("Loading data from database...");

            await using EstateDbContext context = await DbContextFactory.CreateDbContextAsync(ct);

            // Check sync metadata first
            DataSyncMetadata? metadata = await context.SyncMetadata
                .AsNoTracking()
                .OrderBy(m => m.Id)
                .FirstOrDefaultAsync(ct);

            if (metadata is null)
            {
                Logger.LogInformation("No sync metadata found in database");
                return (null, null);
            }

            // Check if database has data
            if (!await context.Estates.AsNoTracking().AnyAsync(ct))
            {
                Logger.LogInformation("Database is empty, no data to load");
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

            // Deserialize work order categories from JSON
            ImmutableArray<WorkOrderCategoryNode> workOrderCategories = DeserializeCategories(metadata.WorkOrderCategoriesJson);

            // Create snapshot
            DataSnapshot snapshot = new(
                estates: [.. estates],
                buildings: [.. buildings],
                floors: [.. floors],
                rooms: [.. rooms],
                buildingAscendants: buildingAscendants,
                refreshUtc: metadata.LastRefreshUtc,
                workOrderCategories: workOrderCategories
            );

            Logger.LogInformation(
                "Loaded from database: {EstateCount} estates, {BuildingCount} buildings, {FloorCount} floors, {RoomCount} rooms, {CategoryCount} work order categories",
                estates.Count, buildings.Count, floors.Count, rooms.Count, workOrderCategories.Length);

            return (snapshot, metadata.LastRefreshUtc);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load data from database");
            return (null, null);
        }
    }

    public abstract Task SaveAsync(DataSnapshot snapshot, DateTimeOffset refreshTime, CancellationToken ct = default);

    protected static List<BuildingAscendantDbEntity> ToDbEntities(
        IReadOnlyDictionary<int, BuildingAscendantTriplet> ascendants)
    {
        return [.. ascendants.Select(kvp => new BuildingAscendantDbEntity
        {
            BuildingId = kvp.Key,
            EstateAscendantId = kvp.Value.Estate?.Id,
            EstateAscendantName = kvp.Value.Estate?.Name,
            EstateAscendantPopularName = kvp.Value.Estate?.PopularName,
            EstateAscendantGeoLat = kvp.Value.Estate?.GeoLocation?.Lat,
            EstateAscendantGeoLon = kvp.Value.Estate?.GeoLocation?.Lon,
            RegionAscendantId = kvp.Value.Region?.Id,
            RegionAscendantName = kvp.Value.Region?.Name,
            RegionAscendantPopularName = kvp.Value.Region?.PopularName,
            RegionAscendantGeoLat = kvp.Value.Region?.GeoLocation?.Lat,
            RegionAscendantGeoLon = kvp.Value.Region?.GeoLocation?.Lon,
            OrganizationAscendantId = kvp.Value.Organization?.Id,
            OrganizationAscendantName = kvp.Value.Organization?.Name,
            OrganizationAscendantPopularName = kvp.Value.Organization?.PopularName,
            OrganizationAscendantGeoLat = kvp.Value.Organization?.GeoLocation?.Lat,
            OrganizationAscendantGeoLon = kvp.Value.Organization?.GeoLocation?.Lon,
        })];
    }

    #region Entity Relationships

    protected static void BuildEntityRelationships(
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

    protected static BuildingAscendantTriplet ToDomainTriplet(BuildingAscendantDbEntity entity)
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

    #region Work Order Categories JSON

    private static readonly JsonSerializerOptions _categoryJsonOptions = new(JsonSerializerDefaults.Web);

    protected static string SerializeCategories(ImmutableArray<WorkOrderCategoryNode> categories)
    {
        if (categories.IsDefaultOrEmpty)
        {
            return "[]";
        }

        return JsonSerializer.Serialize(categories, _categoryJsonOptions);
    }

    private static ImmutableArray<WorkOrderCategoryNode> DeserializeCategories(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return [];
        }

        List<WorkOrderCategoryNode>? list = JsonSerializer.Deserialize<List<WorkOrderCategoryNode>>(json, _categoryJsonOptions);
        return list is { Count: > 0 } ? [.. list] : [];
    }

    #endregion
}
