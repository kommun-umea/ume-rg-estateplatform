using Umea.se.EstateService.Logic.Models;
using Umea.se.EstateService.Shared.Data;
using Umea.se.EstateService.Shared.Data.Entities;
using Umea.se.EstateService.Shared.Models;

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

        return Task.FromResult<IReadOnlyList<BuildingInfoModel>>([.. buildings.Select(MapBuildingWithAscendants)]);
    }

    private BuildingInfoModel MapBuildingWithAscendants(BuildingEntity building)
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

        return MapEntityToBuildingInfoModel(building, estateAsc, regionAsc, orgAsc);
    }

    public Task<BuildingInfoModel?> GetBuildingByIdAsync(int buildingId, CancellationToken cancellationToken = default)
    {
        ValidatePositiveId(buildingId, nameof(buildingId));

        dataStore.BuildingsById.TryGetValue(buildingId, out BuildingEntity? entity);

        if (entity is null)
        {
            return Task.FromResult<BuildingInfoModel?>(null);
        }

        return Task.FromResult<BuildingInfoModel?>(MapBuildingWithAscendants(entity));
    }

    public Task<IReadOnlyList<RoomModel>> GetBuildingWorkspacesAsync(int buildingId, int? floorId = null, QueryArgs? queryArgs = null, CancellationToken cancellationToken = default)
    {
        ValidatePositiveId(buildingId, nameof(buildingId));

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
            return MapRoomEntityToModel(r, building, floor);
        })];

        return Task.FromResult<IReadOnlyList<RoomModel>>(payload);
    }

    public Task<IReadOnlyList<FloorInfoModel>> GetBuildingFloorsAsync(int buildingId, bool includeRooms = false, QueryArgs? floorsQueryArgs = null, QueryArgs? roomsQueryArgs = null, CancellationToken cancellationToken = default)
    {
        ValidatePositiveId(buildingId, nameof(buildingId));

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
                [.. floorList.Select(f => MapFloorEntityToModel(f, building))]);
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
                ? [.. floorRooms.Select(r => MapRoomEntityToModel(r, building, f))]
                : null;
            return MapFloorEntityToModel(f, building, roomModels);
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

        return Task.FromResult<IReadOnlyList<RoomModel>>([.. rooms.Select(r => MapRoomEntityToModel(r, buildingsById, floorsById))]);
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

        return Task.FromResult<IReadOnlyList<EstateModel>>([.. estates.Select(e => MapEstateToModel(e, includeBuildings))]);
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

    public Task<EstateModel?> GetEstateByIdAsync(int estateId, bool includeBuildings = false, CancellationToken cancellationToken = default)
    {
        ValidatePositiveId(estateId, nameof(estateId));

        dataStore.EstatesById.TryGetValue(estateId, out EstateEntity? estate);

        EstateModel? result = estate is not null
            ? MapEstateToModel(estate, includeBuildings)
            : null;

        return Task.FromResult(result);
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
            return MapFloorEntityToModel(f, building);
        })]);
    }

    private static FloorInfoModel MapFloorEntityToModel(FloorEntity floor, BuildingEntity? building, IReadOnlyList<RoomModel>? rooms = null)
    {
        return new FloorInfoModel
        {
            Id = floor.Id,
            Uid = floor.Uid,
            Name = floor.Name,
            PopularName = floor.PopularName,
            Height = floor.Height,
            GrossArea = floor.GrossArea,
            NetArea = floor.NetArea,
            BuildingId = floor.BuildingId,
            BuildingName = building?.Name,
            BuildingPopularName = building?.PopularName,
            Rooms = rooms
        };
    }

    private static RoomModel MapRoomEntityToModel(RoomEntity room, BuildingEntity? building, FloorEntity? floor)
    {
        return new RoomModel
        {
            Id = room.Id,
            Name = room.Name,
            PopularName = room.PopularName,
            GrossArea = room.GrossArea,
            NetArea = room.NetArea,
            Capacity = room.Capacity,
            BuildingId = room.BuildingId,
            BuildingName = building?.Name,
            BuildingPopularName = building?.PopularName,
            FloorId = room.FloorId,
            FloorName = floor?.Name,
            FloorPopularName = floor?.PopularName
        };
    }

    private static RoomModel MapRoomEntityToModel(
        RoomEntity room,
        IReadOnlyDictionary<int, BuildingEntity> buildingsById,
        IReadOnlyDictionary<int, FloorEntity> floorsById)
    {
        buildingsById.TryGetValue(room.BuildingId, out BuildingEntity? building);

        FloorEntity? floor = null;
        if (room.FloorId.HasValue)
        {
            floorsById.TryGetValue(room.FloorId.Value, out floor);
        }

        return MapRoomEntityToModel(room, building, floor);
    }

    private static EstateModel MapEstateToModel(EstateEntity estate, bool includeBuildings = true)
    {
        ExternalOwnerInfoModel? externalOwnerInfo = null;
        if (!string.IsNullOrEmpty(estate.ExternalOwnerStatus) ||
            !string.IsNullOrEmpty(estate.ExternalOwnerName) ||
            !string.IsNullOrEmpty(estate.ExternalOwnerNote))
        {
            externalOwnerInfo = new ExternalOwnerInfoModel
            {
                Status = estate.ExternalOwnerStatus,
                Name = estate.ExternalOwnerName,
                Note = estate.ExternalOwnerNote
            };
        }

        return new EstateModel
        {
            Id = estate.Id,
            Uid = estate.Uid,
            Name = estate.Name,
            PopularName = estate.PopularName,
            GrossArea = estate.GrossArea,
            NetArea = estate.NetArea,
            Address = estate.Address,
            GeoLocation = estate.GeoLocation,
            BuildingCount = estate.BuildingCount,
            Buildings = includeBuildings ? [.. estate.Buildings.Select(MapEntityToBuildingModel)] : [],
            ExtendedProperties = new EstateExtendedPropertiesModel
            {
                PropertyDesignation = estate.PropertyDesignation,
                OperationalArea = estate.OperationalArea,
                AdministrativeArea = estate.AdministrativeArea,
                MunicipalityArea = estate.MunicipalityArea,
                ExternalOwnerInfo = externalOwnerInfo
            }
        };
    }

    private static BuildingModel MapEntityToBuildingModel(BuildingEntity building)
    {
        BuildingModel model = new()
        {
            Id = building.Id,
            Uid = building.Uid,
            Name = building.Name,
            PopularName = building.PopularName,
            Address = building.Address,
            GeoLocation = building.GeoLocation,
        };

        return model;
    }

    private static BuildingInfoModel MapEntityToBuildingInfoModel(
        BuildingEntity building,
        BuildingAscendantModel? estateAscendant = null,
        BuildingAscendantModel? regionAscendant = null,
        BuildingAscendantModel? organizationAscendant = null)
    {
        BuildingNoticeBoardModel? noticeBoardModel = null;
        if (building.NoticeBoard != null)
        {
            noticeBoardModel = new BuildingNoticeBoardModel
            {
                Text = building.NoticeBoard.Text,
                StartDate = building.NoticeBoard.StartDate,
                EndDate = building.NoticeBoard.EndDate
            };
        }

        // Always include extended properties (they're always available from cache)
        ExternalOwnerInfoModel? externalOwnerInfo = null;
        if (!string.IsNullOrEmpty(building.ExternalOwnerStatus) ||
            !string.IsNullOrEmpty(building.ExternalOwnerName) ||
            !string.IsNullOrEmpty(building.ExternalOwnerNote))
        {
            externalOwnerInfo = new ExternalOwnerInfoModel
            {
                Status = building.ExternalOwnerStatus,
                Name = building.ExternalOwnerName,
                Note = building.ExternalOwnerNote
            };
        }

        BuildingExtendedPropertiesModel extendedProperties = new()
        {
            BlueprintAvailable = building.BlueprintAvailable,
            YearOfConstruction = building.YearOfConstruction,
            ExternalOwnerInfo = externalOwnerInfo,
            PropertyDesignation = building.PropertyDesignation,
            NoticeBoard = noticeBoardModel,
            ContactPersons = building.ContactPersons
        };

        // ImageUrl logic:
        // - null ImageIds = unknown (not yet fetched) → provide URL (will lazy-load on request)
        // - empty ImageIds = confirmed no images → null URL
        // - has ImageIds = has images → provide URL
        string? imageUrl = building.ImageIds is { Count: 0 }
            ? null
            : $"/api/buildings/{building.Id}/image";

        BuildingInfoModel model = new()
        {
            // Identity
            Id = building.Id,
            Uid = building.Uid,

            // Basic
            Name = building.Name,
            PopularName = building.PopularName,

            // Location
            Address = building.Address,
            GeoLocation = building.GeoLocation,

            // Measurements
            GrossArea = building.GrossArea,
            NetArea = building.NetArea,

            // Hierarchy - not populated here
            Estate = estateAscendant,
            Region = regionAscendant,
            Organization = organizationAscendant,

            // Extended
            ExtendedProperties = extendedProperties,

            // Workspace stats
            NumFloors = building.NumFloors,
            NumRooms = building.NumRooms,

            // Image
            ImageUrl = imageUrl
        };

        return model;
    }

    #region Private Helper Methods

    private static void ValidatePositiveId(int id, string paramName)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, "ID must be a positive number.");
        }
    }

    #endregion

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
