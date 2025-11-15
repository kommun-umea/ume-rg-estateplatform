using Umea.se.EstateService.Logic.Data.Entities;
using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.Logic.Mappers;
using Umea.se.EstateService.ServiceAccess.Data;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enum;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Logic.Handlers;

public class PythagorasDataHandler(IDataStore dataStore, IPythagorasClient pythagorasClient) : IPythagorasHandlerV2
{
    // NOTE ON ASCENDANT DATA (Estate/Region/Organization)
    // ---------------------------------------------------
    // The v2 handler is in a transition phase from direct Pythagoras API calls to using
    // the in-memory IDataStore snapshots populated by PythagorasDataRefreshService.
    //
    // For buildings specifically:
    //  - GetBuildingsAsync and GetBuildingByIdAsync now read from IDataStore.Buildings and
    //    map entities via MapEntityToBuildingInfoModel.
    //  - Workspace statistics (NumFloors / NumRooms) and extended properties are provided
    //    from the data store.
    //
    // However, BuildingIncludeOptions.Ascendants is currently NOT honored by these
    // data-store-backed methods: the Estate / Region / Organization properties on
    // BuildingInfoModel are left null and we do not call the Pythagoras API to populate
    // ascendants. This is a deliberate first step to remove API dependencies for the core
    // building payload; ascendant support may be reintroduced later, either by:
    //  - deriving estate-level ascendants from EstateEntity / navigation data in IDataStore, or
    //  - using a hybrid approach that still queries Pythagoras for full hierarchy.

    private static readonly IReadOnlyCollection<int> _estateCalculatedPropertyIds = Array.AsReadOnly(
    [
        (int)PropertyCategoryId.OperationalArea,
        (int)PropertyCategoryId.MunicipalityArea,
        (int)PropertyCategoryId.PropertyDesignation
    ]);

    public async Task<IReadOnlyList<BuildingInfoModel>> GetBuildingsAsync(int[]? buildingIds = null, int? estateId = null, BuildingIncludeOptions includeOptions = BuildingIncludeOptions.None, QueryArgs? queryArgs = null, CancellationToken cancellationToken = default)
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

        return [.. buildings.Select(b => MapEntityToBuildingInfoModel(b))];
    }

    public async Task<BuildingInfoModel?> GetBuildingByIdAsync(int buildingId, BuildingIncludeOptions includeOptions = BuildingIncludeOptions.None, CancellationToken cancellationToken = default)
    {
        ValidatePositiveId(buildingId, nameof(buildingId));

        BuildingEntity? entity = dataStore.Buildings.FirstOrDefault(b => b.Id == buildingId);

        if (entity is null)
        {
            return null;
        }

        BuildingInfoModel building = MapEntityToBuildingInfoModel(entity);

        return building;
    }

    public async Task<IReadOnlyList<RoomModel>> GetBuildingWorkspacesAsync(int buildingId, int? floorId = null, QueryArgs? queryArgs = null, CancellationToken cancellationToken = default)
    {
        ValidatePositiveId(buildingId, nameof(buildingId));

        IEnumerable<RoomEntity> rooms = dataStore.Rooms.Where(r => r.BuildingId == buildingId);

        if (floorId.HasValue)
        {
            rooms = rooms.Where(r => r.FloorId == floorId.Value);
        }

        if (!string.IsNullOrWhiteSpace(queryArgs?.SearchTerm))
        {
            rooms = ApplySearch(rooms, queryArgs.SearchTerm!, r => r.Name, r => r.PopularName);
        }

        rooms = ApplyQueryOptions(rooms, queryArgs);

        Dictionary<int, BuildingEntity> buildingsById = dataStore.Buildings.ToDictionary(b => b.Id);
        Dictionary<int, FloorEntity> floorsById = dataStore.Floors.ToDictionary(f => f.Id);

        RoomModel[] payload = [.. rooms.Select(r => MapRoomEntityToModel(r, buildingsById, floorsById))];

        return payload;
    }

    /*
    private async Task<IReadOnlyList<BuildingAscendantModel>> GetBuildingAscendantsAsync(int buildingId, CancellationToken cancellationToken)
    {
        ValidatePositiveId(buildingId, nameof(buildingId));
        PythagorasQuery<BuildingAscendant> query = new PythagorasQuery<BuildingAscendant>()
            .WithQueryParameter("navigationId", 2)
            .WithQueryParameter("includeSelf", false);

        IReadOnlyList<BuildingAscendant> payload = await pythagorasClient
            .GetBuildingAscendantsAsync(buildingId, query, cancellationToken)
            .ConfigureAwait(false);

        return PythagorasBuildingAscendantMapper.ToModel(payload);
    }
    */

    public async Task<IReadOnlyList<FloorInfoModel>> GetBuildingFloorsAsync(int buildingId, bool includeRooms = false, QueryArgs? floorsQueryArgs = null, QueryArgs? roomsQueryArgs = null, CancellationToken cancellationToken = default)
    {
        ValidatePositiveId(buildingId, nameof(buildingId));

        IEnumerable<FloorEntity> floors = dataStore.Floors.Where(f => f.BuildingId == buildingId);
        if (!string.IsNullOrWhiteSpace(floorsQueryArgs?.SearchTerm))
        {
            floors = ApplySearch(floors, floorsQueryArgs.SearchTerm!, f => f.Name, f => f.PopularName);
        }

        floors = ApplyQueryOptions(floors, floorsQueryArgs);

        List<FloorEntity> floorList = [.. floors];

        if (floorList.Count == 0)
        {
            return [];
        }

        Dictionary<int, BuildingEntity> buildingsById = dataStore.Buildings.ToDictionary(b => b.Id);
        buildingsById.TryGetValue(buildingId, out BuildingEntity? building);

        if (!includeRooms)
        {
            return [.. floorList.Select(f => new FloorInfoModel
            {
                Id = f.Id,
                Uid = f.Uid,
                Name = f.Name,
                PopularName = f.PopularName,
                Height = f.Height,
                GrossArea = f.GrossArea,
                NetArea = f.NetArea,
                BuildingId = f.BuildingId,
                BuildingName = building?.Name,
                BuildingPopularName = building?.PopularName
            })];
        }

        // Pre-load rooms for this building and group by FloorId to avoid per-floor queries
        IEnumerable<RoomEntity> allRoomsForBuilding = dataStore.Rooms.Where(r => r.BuildingId == buildingId);
        if (roomsQueryArgs?.Paging is not null)
        {
            // Apply paging to the flattened rooms list when includeRooms=true
            allRoomsForBuilding = ApplyQueryOptions(allRoomsForBuilding, roomsQueryArgs);
        }

        Dictionary<int, List<RoomEntity>> roomsByFloorId = allRoomsForBuilding
            .Where(r => r.FloorId.HasValue)
            .GroupBy(r => r.FloorId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        Dictionary<int, FloorEntity> floorsById = floorList.ToDictionary(f => f.Id);

        FloorInfoModel[] result = new FloorInfoModel[floorList.Count];
        for (int i = 0; i < floorList.Count; i++)
        {
            FloorEntity floor = floorList[i];

            roomsByFloorId.TryGetValue(floor.Id, out List<RoomEntity>? floorRooms);
            IReadOnlyList<RoomModel>? roomModels = floorRooms is { Count: > 0 }
                ? floorRooms.Select(r => MapRoomEntityToModel(r, buildingsById, floorsById)).ToArray()
                : null;

            result[i] = new FloorInfoModel
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
                Rooms = roomModels
            };
        }

        return result;
    }

    public async Task<IReadOnlyList<RoomModel>> GetRoomsAsync(int[]? roomIds = null, int? buildingId = null, int? floorId = null, QueryArgs? queryArgs = null, CancellationToken cancellationToken = default)
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

        Dictionary<int, BuildingEntity> buildingsById = dataStore.Buildings.ToDictionary(b => b.Id);
        Dictionary<int, FloorEntity> floorsById = dataStore.Floors.ToDictionary(f => f.Id);

        return [.. rooms.Select(r => MapRoomEntityToModel(r, buildingsById, floorsById))];
    }

    public async Task<IReadOnlyDictionary<int, BuildingWorkspaceStatsModel>> GetBuildingWorkspaceStatsAsync(QueryArgs? queryArgs = null, CancellationToken cancellationToken = default)
    {
        PythagorasQuery<Workspace> query = new();
        query = ApplyQueryArgs(query, queryArgs);

        IReadOnlyList<Workspace> workspaces = await pythagorasClient
            .GetWorkspacesAsync(query, cancellationToken)
            .ConfigureAwait(false);

        if (workspaces.Count == 0)
        {
            return new Dictionary<int, BuildingWorkspaceStatsModel>();
        }

        Dictionary<int, WorkspaceStatsAccumulator> accumulators = new(workspaces.Count);

        foreach (Workspace workspace in workspaces)
        {
            if (workspace.BuildingId is not int buildingId || buildingId <= 0)
            {
                continue;
            }

            if (!accumulators.TryGetValue(buildingId, out WorkspaceStatsAccumulator? accumulator))
            {
                accumulator = new WorkspaceStatsAccumulator();
                accumulators[buildingId] = accumulator;
            }

            accumulator.RoomCount++;

            if (workspace.FloorId is int floorId && floorId > 0)
            {
                accumulator.FloorIds.Add(floorId);
            }
        }

        Dictionary<int, BuildingWorkspaceStatsModel> result = new(accumulators.Count);

        foreach (KeyValuePair<int, WorkspaceStatsAccumulator> entry in accumulators)
        {
            WorkspaceStatsAccumulator accumulator = entry.Value;
            int buildingId = entry.Key;
            result[buildingId] = new BuildingWorkspaceStatsModel
            {
                BuildingId = buildingId,
                NumberOfRooms = accumulator.RoomCount,
                NumberOfFloors = accumulator.FloorIds.Count
            };
        }

        return result;
    }

    public async Task<IReadOnlyList<EstateModel>> GetEstatesWithBuildingsAsync(bool includeBuildings = true, QueryArgs? queryArgs = null, CancellationToken cancellationToken = default)
    {
        IEnumerable<EstateEntity> estates = dataStore.Estates;
        if (!string.IsNullOrWhiteSpace(queryArgs?.SearchTerm))
        {
            estates = ApplySearch(estates, queryArgs.SearchTerm!, e => e.Name, e => e.PopularName);
        }

        estates = ApplyQueryOptions(estates, queryArgs);

        return [.. estates.Select(MapEstateToModel)];
    }

    public async Task<IReadOnlyList<EstateModel>> GetEstatesWithPropertiesAsync(IReadOnlyCollection<int>? estateIds = null, IReadOnlyCollection<int>? propertyIds = null, int? navigationId = null, CancellationToken cancellationToken = default)
    {
        NavigationFolderUiListDataRequest request = new()
        {
            NavigationFolderIds = ValidateAndCloneIds(estateIds, nameof(estateIds)),
            PropertyIds = GetEffectivePropertyIds(propertyIds, _estateCalculatedPropertyIds, nameof(propertyIds)),
            NavigationId = navigationId ?? NavigationType.UmeaKommun,
            IncludePropertyValues = true
        };

        UiListDataResponse<NavigationFolder> response = await pythagorasClient
            .PostNavigationFolderUiListDataAsync(request, cancellationToken)
            .ConfigureAwait(false);

        if (response.Data.Count == 0)
        {
            return [];
        }

        List<EstateModel> models = new(response.Data.Count);
        foreach (NavigationFolder estate in response.Data)
        {
            EstateExtendedPropertiesModel? extendedProperties = PythagorasEstatePropertyMapper.ToExtendedPropertiesModel(estate.PropertyValues);
            models.Add(PythagorasEstateMapper.ToModel(estate, extendedProperties));
        }

        return models;
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

    public async Task<EstateModel?> GetEstateByIdAsync(int estateId, bool includeBuildings = false, CancellationToken cancellationToken = default)
    {
        ValidatePositiveId(estateId, nameof(estateId));

        IEnumerable<EstateEntity> estates = dataStore.Estates;
        EstateEntity? estate = estates.FirstOrDefault(e => e.Id == estateId);

        return estate is not null
            ? MapEstateToModel(estate)
            : null;

        /*
        PythagorasQuery<NavigationFolder> query = new PythagorasQuery<NavigationFolder>()
            .Where(folder => folder.Id, estateId)
            .Where(folder => folder.TypeId, NavigationFolderType.Estate)
            .WithQueryParameter("navigationId", NavigationType.UmeaKommun)
            .WithQueryParameter("includeAscendantBuildings", includeBuildings);

        IReadOnlyList<NavigationFolder> payload = await pythagorasClient
            .GetNavigationFoldersAsync(query, cancellationToken)
            .ConfigureAwait(false);

        if (payload.Count == 0)
        {
            return null;
        }

        NavigationFolder estateDto = payload[0];

        CalculatedPropertyValueRequest propertyRequest = new()
        {
            PropertyIds = _estateCalculatedPropertyIds,
            NavigationId = NavigationType.UmeaKommun
        };

        IReadOnlyDictionary<PropertyCategoryId, CalculatedPropertyValueDto> properties = await GetEstateCalculatedPropertyValuesInternalAsync(
                estateId,
                propertyRequest,
                cancellationToken)
            .ConfigureAwait(false);

        EstateExtendedPropertiesModel? extendedProperties = PythagorasEstatePropertyMapper.ToExtendedPropertiesModel(properties);

        return PythagorasEstateMapper.ToModel(estateDto, extendedProperties);
        */
    }

    public async Task<IReadOnlyList<FloorInfoModel>> GetFloorsAsync(int[]? floorIds = null, QueryArgs? queryArgs = null, CancellationToken cancellationToken = default)
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

        Dictionary<int, BuildingEntity> buildingsById = dataStore.Buildings.ToDictionary(b => b.Id);

        return [.. floors.Select(f =>
        {
            buildingsById.TryGetValue(f.BuildingId, out BuildingEntity? building);

            return new FloorInfoModel
            {
                Id = f.Id,
                Uid = f.Uid,
                Name = f.Name,
                PopularName = f.PopularName,
                Height = f.Height,
                GrossArea = f.GrossArea,
                NetArea = f.NetArea,
                BuildingId = f.BuildingId,
                BuildingName = building?.Name,
                BuildingPopularName = building?.PopularName
            };
        })];
    }

    private sealed class WorkspaceStatsAccumulator
    {
        public HashSet<int> FloorIds { get; } = [];
        public int RoomCount { get; set; }
    }

    private static RoomModel MapRoomEntityToModel(
        RoomEntity room,
        Dictionary<int, BuildingEntity> buildingsById,
        Dictionary<int, FloorEntity> floorsById)
    {
        buildingsById.TryGetValue(room.BuildingId, out BuildingEntity? building);

        FloorEntity? floor = null;
        if (room.FloorId.HasValue)
        {
            floorsById.TryGetValue(room.FloorId.Value, out floor);
        }

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

    private static EstateModel MapEstateToModel(EstateEntity estate)
    {
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
            Buildings = [.. estate.Buildings.Select(MapEntityToBuildingModel)],
            ExtendedProperties = new EstateExtendedPropertiesModel
            {
                PropertyDesignation = estate.PropertyDesignation,
                OperationalArea = estate.OperationalArea,
                MunicipalityArea = estate.MunicipalityArea
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

        // Check if there are any extended properties to include
        bool hasExtendedProperties = !string.IsNullOrEmpty(building.YearOfConstruction)
            || !string.IsNullOrEmpty(building.ExternalOwner)
            || !string.IsNullOrEmpty(building.PropertyDesignation)
            || building.NoticeBoard != null;

        BuildingExtendedPropertiesModel? extendedProperties = hasExtendedProperties
            ? new BuildingExtendedPropertiesModel
            {
                YearOfConstruction = building.YearOfConstruction,
                ExternalOwner = building.ExternalOwner,
                PropertyDesignation = building.PropertyDesignation,
                NoticeBoard = noticeBoardModel
            }
            : null;

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
            NumRooms = building.NumRooms
        };

        return model;
    }

    #region Private Helper Methods

    /// <summary>
    /// Applies QueryArgs (search, paging, ordering) to a PythagorasQuery.
    /// </summary>
    private static PythagorasQuery<T> ApplyQueryArgs<T>(PythagorasQuery<T> query, QueryArgs? queryArgs) where T : class
    {
        if (queryArgs is null)
        {
            return query;
        }

        // Validate paging options early
        queryArgs.Paging?.Validate();

        if (!string.IsNullOrWhiteSpace(queryArgs.SearchTerm))
        {
            query = query.GeneralSearch(queryArgs.SearchTerm.Trim());
        }

        if (queryArgs.Paging is not null)
        {
            if (queryArgs.Paging.Skip.HasValue && queryArgs.Paging.Skip > 0)
            {
                query = query.Skip(queryArgs.Paging.Skip.Value);
            }

            if (queryArgs.Paging.Take.HasValue && queryArgs.Paging.Take > 0)
            {
                query = query.Take(queryArgs.Paging.Take.Value);
            }
        }

        // Note: Ordering support can be added here when needed
        // if (queryArgs.Ordering is not null && !string.IsNullOrWhiteSpace(queryArgs.Ordering.FieldName))
        // {
        //     query = query.OrderBy(queryArgs.Ordering.FieldName, queryArgs.Ordering.Direction == SortingDirection.Ascending);
        // }

        return query;
    }

    private static void ValidatePositiveId(int id, string paramName)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, "ID must be a positive number.");
        }
    }

    private static IReadOnlyCollection<int>? ValidateAndCloneIds(IReadOnlyCollection<int>? ids, string paramName)
    {
        if (ids is not { Count: > 0 })
        {
            return null;
        }

        if (ids.Any(id => id <= 0))
        {
            throw new ArgumentOutOfRangeException(paramName, "All IDs in the collection must be positive.");
        }

        return [.. ids];
    }

    private static IReadOnlyCollection<int> GetEffectivePropertyIds(IReadOnlyCollection<int>? providedIds, IReadOnlyCollection<int> defaultIds, string paramName)
    {
        if (providedIds is not { Count: > 0 })
        {
            return defaultIds;
        }

        if (providedIds.Any(id => id <= 0))
        {
            throw new ArgumentOutOfRangeException(paramName, "All property IDs must be positive.");
        }

        return [.. providedIds];
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
