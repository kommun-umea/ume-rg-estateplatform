using Umea.se.EstateService.Logic.Models;
using Umea.se.EstateService.Shared.Data;
using Umea.se.EstateService.Shared.Data.Entities;
using Umea.se.EstateService.Shared.Exceptions;
using Umea.se.EstateService.Shared.Models;
using Umea.se.EstateService.Shared.Search;

namespace Umea.se.EstateService.Logic.Handlers;

public class EstateDataQueryHandler(IDataStore dataStore) : IEstateDataQueryHandler
{
    // NOTE ON ASCENDANT DATA (Estate/Region/Organization)
    // ---------------------------------------------------
    // During background refresh, PythagorasDataRefreshService:
    //  - Fetches the full navigation hierarchy from Pythagoras API
    //  - Pre-computes ascendant triplets for all buildings
    //  - Stores them as a dictionary in IDataStore for O(1) lookup
    //
    // Ascendants are always included from the cache when available.

    /// <summary>
    /// Stamps ImageUrl on building documents from live entity data
    /// (kept fresh by write-through from BuildingBackgroundCache).
    /// </summary>
    public void StampBuildingImageUrls(IEnumerable<PythagorasDocument> documents)
    {
        foreach (PythagorasDocument doc in documents)
        {
            if (doc.Type != NodeType.Building)
            {
                continue;
            }

            if (dataStore.BuildingsById.TryGetValue(doc.Id, out BuildingEntity? building))
            {
                doc.ImageUrl = GetBuildingImageUrl(building);
            }
        }
    }

    internal static string? GetBuildingImageUrl(BuildingEntity building)
    {
        return building.ImageIds is { Count: 0 }
            ? null
            : $"/api/buildings/{building.Id}/image";
    }

    public Task<IReadOnlyList<BuildingInfoModel>> GetBuildingsAsync(int[]? buildingIds = null, int? estateId = null, QueryArgs? queryArgs = null, CancellationToken cancellationToken = default)
    {
        IEnumerable<BuildingEntity> buildings = dataStore.Buildings;

        if (buildingIds is { Length: > 0 })
        {
            HashSet<int> idSet = [.. buildingIds];
            buildings = buildings.Where(b => idSet.Contains(b.Id));
        }

        if (estateId.HasValue)
        {
            buildings = buildings.Where(b => b.EstateId == estateId.Value);
        }

        if (!string.IsNullOrWhiteSpace(queryArgs?.SearchTerm))
        {
            buildings = ApplySearch(buildings, queryArgs.SearchTerm!, b => b.Name, b => b.PopularName, b => b.PropertyDesignation);
        }

        buildings = ApplyQueryOptions(buildings, queryArgs);

        return Task.FromResult<IReadOnlyList<BuildingInfoModel>>([.. buildings.Select(MapBuildingInfoWithAscendants)]);
    }

    private BuildingInfoModel MapBuildingInfoWithAscendants(BuildingEntity building)
    {
        BuildingAscendantModel? estateAsc = null;
        BuildingAscendantModel? regionAsc = null;
        BuildingAscendantModel? orgAsc = null;

        // Always include ascendants from cache
        if (dataStore.BuildingAscendants.TryGetValue(building.Id, out BuildingAscendantTriplet? triplet))
        {
            estateAsc = triplet.Estate;
            regionAsc = triplet.Region;
            orgAsc = triplet.Organization;
        }

        return EstateModelMapper.MapBuildingInfo(building, estateAsc, regionAsc, orgAsc);
    }

    public Task<BuildingInfoModel> GetBuildingByIdAsync(int buildingId, CancellationToken cancellationToken = default)
    {
        if (!dataStore.BuildingsById.TryGetValue(buildingId, out BuildingEntity? entity))
        {
            throw new EntityNotFoundException($"Building with id {buildingId} not found.");
        }

        return Task.FromResult(MapBuildingInfoWithAscendants(entity));
    }

    public Task<IReadOnlyList<RoomModel>> GetBuildingWorkspacesAsync(int buildingId, int? floorId = null, QueryArgs? queryArgs = null, CancellationToken cancellationToken = default)
    {
        dataStore.BuildingsById.TryGetValue(buildingId, out BuildingEntity? building);

        IEnumerable<RoomEntity> rooms = building?.Rooms ?? [];

        if (floorId.HasValue)
        {
            rooms = rooms.Where(r => r.FloorId == floorId.Value);
        }

        if (!string.IsNullOrWhiteSpace(queryArgs?.SearchTerm))
        {
            rooms = ApplySearch(rooms, queryArgs.SearchTerm!, r => r.Name, r => r.PopularName);
        }

        rooms = ApplyQueryOptions(rooms, queryArgs);

        IReadOnlyDictionary<int, FloorEntity> floorsById = dataStore.FloorsById;

        RoomModel[] payload = [.. rooms.Select(r =>
        {
            FloorEntity? floor = r.FloorId.HasValue ? floorsById.GetValueOrDefault(r.FloorId.Value) : null;
            return EstateModelMapper.MapRoom(r, building, floor);
        })];

        return Task.FromResult<IReadOnlyList<RoomModel>>(payload);
    }

    public Task<IReadOnlyList<FloorInfoModel>> GetBuildingFloorsAsync(int buildingId, bool includeRooms = false, QueryArgs? floorsQueryArgs = null, QueryArgs? roomsQueryArgs = null, CancellationToken cancellationToken = default)
    {
        dataStore.BuildingsById.TryGetValue(buildingId, out BuildingEntity? building);

        IEnumerable<FloorEntity> floors = building?.Floors ?? [];

        if (!string.IsNullOrWhiteSpace(floorsQueryArgs?.SearchTerm))
        {
            floors = ApplySearch(floors, floorsQueryArgs.SearchTerm!, f => f.Name, f => f.PopularName);
        }

        floors = ApplyQueryOptions(floors, floorsQueryArgs);
        List<FloorEntity> floorList = [.. floors];

        if (floorList.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<FloorInfoModel>>([]);
        }

        if (!includeRooms)
        {
            return Task.FromResult<IReadOnlyList<FloorInfoModel>>(
                [.. floorList.Select(f => EstateModelMapper.MapFloor(f, building))]);
        }

        // Group rooms by floor, applying optional paging
        IEnumerable<RoomEntity> allRooms = building?.Rooms ?? [];
        if (roomsQueryArgs is not null)
        {
            allRooms = ApplyQueryOptions(allRooms, roomsQueryArgs);
        }

        Dictionary<int, List<RoomEntity>> roomsByFloorId = allRooms
            .Where(r => r.FloorId.HasValue)
            .GroupBy(r => r.FloorId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        return Task.FromResult<IReadOnlyList<FloorInfoModel>>([.. floorList.Select(f =>
        {
            roomsByFloorId.TryGetValue(f.Id, out List<RoomEntity>? floorRooms);
            IReadOnlyList<RoomModel>? roomModels = floorRooms is { Count: > 0 }
                ? [.. floorRooms.Select(r => EstateModelMapper.MapRoom(r, building, f))]
                : null;
            return EstateModelMapper.MapFloor(f, building, roomModels);
        })]);
    }

    public Task<IReadOnlyList<RoomModel>> GetRoomsAsync(int[]? roomIds = null, int? buildingId = null, int? floorId = null, QueryArgs? queryArgs = null, CancellationToken cancellationToken = default)
    {
        IEnumerable<RoomEntity> rooms = dataStore.Rooms;

        if (roomIds is { Length: > 0 })
        {
            HashSet<int> idSet = [.. roomIds];
            rooms = rooms.Where(r => idSet.Contains(r.Id));
        }
        else
        {
            if (buildingId.HasValue)
            {
                rooms = rooms.Where(r => r.BuildingId == buildingId.Value);
            }

            if (floorId.HasValue)
            {
                rooms = rooms.Where(r => r.FloorId == floorId.Value);
            }

            if (!string.IsNullOrWhiteSpace(queryArgs?.SearchTerm))
            {
                rooms = ApplySearch(rooms, queryArgs.SearchTerm!, r => r.Name, r => r.PopularName);
            }

            rooms = ApplyQueryOptions(rooms, queryArgs);
        }

        IReadOnlyDictionary<int, BuildingEntity> buildingsById = dataStore.BuildingsById;
        IReadOnlyDictionary<int, FloorEntity> floorsById = dataStore.FloorsById;

        return Task.FromResult<IReadOnlyList<RoomModel>>([.. rooms.Select(r => EstateModelMapper.MapRoom(r, buildingsById, floorsById))]);
    }

    public Task<IReadOnlyDictionary<int, BuildingWorkspaceStatsModel>> GetBuildingWorkspaceStatsAsync(QueryArgs? queryArgs = null, CancellationToken cancellationToken = default)
    {
        IEnumerable<BuildingEntity> buildings = dataStore.Buildings;

        if (!string.IsNullOrWhiteSpace(queryArgs?.SearchTerm))
        {
            buildings = ApplySearch(buildings, queryArgs.SearchTerm!, b => b.Name, b => b.PopularName);
        }

        buildings = ApplyQueryOptions(buildings, queryArgs);

        Dictionary<int, BuildingWorkspaceStatsModel> result = buildings
            .ToDictionary(
                b => b.Id,
                b => new BuildingWorkspaceStatsModel
                {
                    BuildingId = b.Id,
                    NumberOfRooms = b.NumRooms,
                    NumberOfFloors = b.NumFloors
                });

        return Task.FromResult<IReadOnlyDictionary<int, BuildingWorkspaceStatsModel>>(result);
    }

    public Task<IReadOnlyList<EstateModel>> GetEstatesWithBuildingsAsync(bool includeBuildings = true, QueryArgs? queryArgs = null, CancellationToken cancellationToken = default)
    {
        IEnumerable<EstateEntity> estates = dataStore.Estates;
        if (!string.IsNullOrWhiteSpace(queryArgs?.SearchTerm))
        {
            estates = ApplySearch(estates, queryArgs.SearchTerm!, e => e.Name, e => e.PopularName);
        }

        estates = ApplyQueryOptions(estates, queryArgs);

        return Task.FromResult<IReadOnlyList<EstateModel>>([.. estates.Select(e => EstateModelMapper.MapEstate(e, includeBuildings))]);
    }

    public Task<IReadOnlyList<BusinessTypeModel>> GetBusinessTypesAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<BusinessTypeModel> businessTypes = [.. dataStore.Buildings
            .Select(b => b.BusinessType)
            .Where(bt => bt is not null)
            .DistinctBy(bt => bt!.Id)
            .Select(bt => bt!)
            .OrderBy(bt => bt.Name)];

        return Task.FromResult(businessTypes);
    }

    public Task<IReadOnlyList<BuildingLocationModel>> GetBuildingGeolocationsAsync(CancellationToken cancellationToken = default)
    {
        BuildingLocationModel[] locations = [.. dataStore.Buildings
            .Where(static b => b.GeoLocation is not null)
            .Select(static b => new BuildingLocationModel
            {
                Id = b.Id,
                GeoLocation = b.GeoLocation
            })];

        return Task.FromResult<IReadOnlyList<BuildingLocationModel>>(locations);
    }

    public Task<EstateModel> GetEstateByIdAsync(int estateId, bool includeBuildings = false, CancellationToken cancellationToken = default)
    {
        if (!dataStore.EstatesById.TryGetValue(estateId, out EstateEntity? estate))
        {
            throw new EntityNotFoundException($"Estate with id {estateId} not found.");
        }

        return Task.FromResult(EstateModelMapper.MapEstate(estate, includeBuildings));
    }

    public Task<IReadOnlyList<FloorInfoModel>> GetFloorsAsync(int[]? floorIds = null, QueryArgs? queryArgs = null, CancellationToken cancellationToken = default)
    {
        IEnumerable<FloorEntity> floors = dataStore.Floors;

        if (floorIds is { Length: > 0 })
        {
            HashSet<int> idSet = [.. floorIds];
            floors = floors.Where(f => idSet.Contains(f.Id));
        }

        if (!string.IsNullOrWhiteSpace(queryArgs?.SearchTerm))
        {
            floors = ApplySearch(floors, queryArgs.SearchTerm!, f => f.Name, f => f.PopularName);
        }

        floors = ApplyQueryOptions(floors, queryArgs);

        IReadOnlyDictionary<int, BuildingEntity> buildingsById = dataStore.BuildingsById;

        return Task.FromResult<IReadOnlyList<FloorInfoModel>>([.. floors.Select(f =>
        {
            buildingsById.TryGetValue(f.BuildingId, out BuildingEntity? building);
            return EstateModelMapper.MapFloor(f, building);
        })]);
    }

    private static IEnumerable<T> ApplyQueryOptions<T>(IEnumerable<T> enumerable, QueryArgs? args)
    {
        ArgumentNullException.ThrowIfNull(enumerable);

        if (args == null)
        {
            return enumerable;
        }

        if (args.Paging != null)
        {
            args.Paging.Validate();

            if (args.Paging.Skip.HasValue)
            {
                enumerable = enumerable.Skip(args.Paging.Skip.Value);
            }

            if (args.Paging.Take.HasValue)
            {
                enumerable = enumerable.Take(args.Paging.Take.Value);
            }
        }

        return enumerable;
    }

    private static IEnumerable<T> ApplySearch<T>(IEnumerable<T> source, string searchTerm, params Func<T, string?>[] selectors)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (string.IsNullOrWhiteSpace(searchTerm) || selectors.Length == 0)
        {
            return source;
        }

        string term = searchTerm.Trim();

        return source.Where(item =>
            selectors.Any(selector =>
            {
                string? value = selector(item);
                return !string.IsNullOrEmpty(value) &&
                       value.Contains(term, StringComparison.OrdinalIgnoreCase);
            }));
    }
}
