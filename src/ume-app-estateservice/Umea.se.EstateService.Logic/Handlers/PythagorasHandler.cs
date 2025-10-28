using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.Logic.Mappers;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enum;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Logic.Handlers;

public class PythagorasHandler(IPythagorasClient pythagorasClient) : IPythagorasHandler
{
    private static readonly IReadOnlyCollection<int> _estateCalculatedPropertyIds = Array.AsReadOnly(
    [
        (int)PropertyCategoryId.OperationalArea,
        (int)PropertyCategoryId.MunicipalityArea,
        (int)PropertyCategoryId.PropertyDesignation
    ]);

    private static readonly IReadOnlyCollection<int> _buildingExtendedPropertyIds = Array.AsReadOnly(
    [
        (int)PropertyCategoryId.ExternalOwner,
        (int)PropertyCategoryId.PropertyDesignation,
        (int)PropertyCategoryId.NoticeBoardText,
        (int)PropertyCategoryId.NoticeBoardStartDate,
        (int)PropertyCategoryId.NoticeBoardEndDate,
        (int)PropertyCategoryId.YearOfConstruction
    ]);

    public async Task<IReadOnlyList<BuildingInfoModel>> GetBuildingsAsync(PythagorasQuery<BuildingInfo>? query = null, CancellationToken cancellationToken = default)
    {
        PythagorasQuery<BuildingInfo> effectiveQuery = (query ?? new PythagorasQuery<BuildingInfo>())
            .WithQueryParameter("navigationId", NavigationType.UmeaKommun);

        IReadOnlyList<BuildingInfo> payload = await pythagorasClient.GetBuildingsAsync(effectiveQuery, cancellationToken).ConfigureAwait(false);
        return PythagorasBuildingInfoMapper.ToModel(payload);
    }

    public async Task<IReadOnlyList<BuildingInfoModel>> GetBuildingsWithPropertiesAsync(IReadOnlyCollection<int>? buildingIds = null, IReadOnlyCollection<int>? propertyIds = null, int? navigationId = null, CancellationToken cancellationToken = default)
    {
        BuildingUiListDataRequest request = new()
        {
            BuildingIds = ValidateAndCloneIds(buildingIds, nameof(buildingIds)),
            PropertyIds = GetEffectivePropertyIds(propertyIds, _buildingExtendedPropertyIds, nameof(propertyIds)),
            NavigationId = navigationId ?? NavigationType.UmeaKommun,
            IncludePropertyValues = true
        };

        UiListDataResponse<BuildingInfo> response = await pythagorasClient
            .PostBuildingUiListDataAsync(request, cancellationToken)
            .ConfigureAwait(false);

        if (response.Data.Count == 0)
        {
            return [];
        }

        List<BuildingInfoModel> models = new(response.Data.Count);
        foreach (BuildingInfo building in response.Data)
        {
            BuildingExtendedPropertiesModel? extendedProperties = PythagorasBuildingInfoMapper.ToExtendedPropertiesModel(building.PropertyValues);
            models.Add(PythagorasBuildingInfoMapper.ToModel(building, extendedProperties));
        }

        return models;
    }

    public async Task<BuildingInfoModel?> GetBuildingByIdAsync(int buildingId, BuildingIncludeOptions includeOptions = BuildingIncludeOptions.None, CancellationToken cancellationToken = default)
    {
        ValidatePositiveId(buildingId, nameof(buildingId));

        PythagorasQuery<BuildingInfo> query = new PythagorasQuery<BuildingInfo>()
            .Where(b => b.Id, buildingId);

        IReadOnlyList<BuildingInfo> payload = await pythagorasClient
            .GetBuildingsAsync(query, cancellationToken)
            .ConfigureAwait(false);

        if (payload.Count == 0)
        {
            return null;
        }

        BuildingExtendedPropertiesModel? extendedProperties = null;
        if (includeOptions.HasFlag(BuildingIncludeOptions.ExtendedProperties))
        {
            IReadOnlyDictionary<PropertyCategoryId, CalculatedPropertyValueDto> properties = await GetBuildingCalculatedPropertyValuesInternalAsync(
                    buildingId,
                    request: null,
                    cancellationToken)
                .ConfigureAwait(false);

            extendedProperties = PythagorasBuildingInfoMapper.ToExtendedPropertiesModel(properties);
        }

        return PythagorasBuildingInfoMapper.ToModel(payload[0], extendedProperties);
    }

    public async Task<IReadOnlyList<BuildingRoomModel>> GetBuildingWorkspacesAsync(int buildingId, PythagorasQuery<Workspace>? query = null, CancellationToken cancellationToken = default)
    {
        ValidatePositiveId(buildingId, nameof(buildingId));
        PythagorasQuery<Workspace> effectiveQuery = (query ?? new PythagorasQuery<Workspace>())
            .WithQueryParameter("buildingId", buildingId);

        IReadOnlyList<Workspace> payload = await pythagorasClient
            .GetWorkspacesAsync(effectiveQuery, cancellationToken)
            .ConfigureAwait(false);

        return PythagorasWorkspaceMapper.ToBuildingModel(payload);
    }

    public async Task<IReadOnlyList<BuildingAscendantModel>> GetBuildingAscendantsAsync(int buildingId, CancellationToken cancellationToken = default)
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

    public async Task<IReadOnlyList<FloorInfoModel>> GetBuildingFloorsAsync(int buildingId, PythagorasQuery<Floor>? floorQuery = null, CancellationToken cancellationToken = default)
    {
        ValidatePositiveId(buildingId, nameof(buildingId));
        IReadOnlyList<Floor> floors = await GetBuildingFloorsInternalAsync(buildingId, floorQuery, cancellationToken).ConfigureAwait(false);

        return floors.Count == 0
            ? []
            : PythagorasFloorInfoMapper.ToModel(floors);
    }

    public async Task<IReadOnlyList<FloorInfoModel>> GetBuildingFloorsWithRoomsAsync(int buildingId, PythagorasQuery<Floor>? floorQuery = null, PythagorasQuery<Workspace>? workspaceQuery = null, CancellationToken cancellationToken = default)
    {
        ValidatePositiveId(buildingId, nameof(buildingId));
        IReadOnlyList<Floor> floors = await GetBuildingFloorsInternalAsync(buildingId, floorQuery, cancellationToken).ConfigureAwait(false);

        if (floors.Count == 0)
        {
            return [];
        }

        IEnumerable<Task<FloorInfoModel>> floorTasks = floors.Select(async floor =>
        {
            if (floor.Id <= 0)
            {
                return PythagorasFloorInfoMapper.ToModel(floor, []);
            }

            PythagorasQuery<Workspace> floorQueryWithFilter = (workspaceQuery ?? new PythagorasQuery<Workspace>())
                .WithQueryParameter("floorId", floor.Id);

            IReadOnlyList<Workspace> workspaceDtos = await pythagorasClient
                .GetWorkspacesAsync(floorQueryWithFilter, cancellationToken)
                .ConfigureAwait(false);

            IReadOnlyList<BuildingRoomModel> rooms = PythagorasWorkspaceMapper.ToBuildingModel(workspaceDtos);
            return PythagorasFloorInfoMapper.ToModel(floor, rooms);
        });

        return await Task.WhenAll(floorTasks).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<Floor>> GetBuildingFloorsInternalAsync(int buildingId, PythagorasQuery<Floor>? floorQuery, CancellationToken cancellationToken)
    {
        ValidatePositiveId(buildingId, nameof(buildingId));

        return await pythagorasClient
            .GetBuildingFloorsAsync(buildingId, floorQuery, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<RoomModel>> GetRoomsAsync(PythagorasQuery<Workspace>? query = null, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Workspace> payload = await pythagorasClient.GetWorkspacesAsync(query, cancellationToken).ConfigureAwait(false);
        return PythagorasWorkspaceMapper.ToModel(payload);
    }

    public async Task<IReadOnlyDictionary<int, BuildingWorkspaceStatsModel>> GetBuildingWorkspaceStatsAsync(PythagorasQuery<Workspace>? query = null, CancellationToken cancellationToken = default)
    {
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

    public async Task<IReadOnlyList<EstateModel>> GetEstatesWithBuildingsAsync(PythagorasQuery<NavigationFolder>? query = null, CancellationToken cancellationToken = default)
    {
        query ??= new PythagorasQuery<NavigationFolder>();
        query = query
            .Where(f => f.TypeId, NavigationFolderType.Estate)
            .WithQueryParameter("navigationId", NavigationType.UmeaKommun)
            .WithQueryParameter("includeAscendantBuildings", true);

        IReadOnlyList<NavigationFolder> payload = await pythagorasClient
            .GetNavigationFoldersAsync(query, cancellationToken)
            .ConfigureAwait(false);

        return PythagorasEstateMapper.ToModel(payload);
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

    public async Task<EstateModel?> GetEstateByIdAsync(int estateId, bool includeBuildings = false, CancellationToken cancellationToken = default)
    {
        ValidatePositiveId(estateId, nameof(estateId));

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
    }
    public async Task<IReadOnlyList<FloorInfoModel>> GetFloorsAsync(PythagorasQuery<Floor>? query = null, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Floor> payload = await pythagorasClient
            .GetFloorsAsync(query, cancellationToken)
            .ConfigureAwait(false);

        return PythagorasFloorInfoMapper.ToModel(payload);
    }

    private async Task<IReadOnlyDictionary<PropertyCategoryId, CalculatedPropertyValueDto>> GetBuildingCalculatedPropertyValuesInternalAsync(int buildingId, CalculatedPropertyValueRequest? request = null, CancellationToken cancellationToken = default)
    {
        ValidatePositiveId(buildingId, nameof(buildingId));

        return await GetCalculatedPropertyValuesInternalAsync(
            ct => pythagorasClient.GetBuildingCalculatedPropertyValuesAsync(buildingId, request, ct),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyDictionary<PropertyCategoryId, CalculatedPropertyValueDto>> GetEstateCalculatedPropertyValuesInternalAsync(int estateId, CalculatedPropertyValueRequest? request = null, CancellationToken cancellationToken = default)
    {
        ValidatePositiveId(estateId, nameof(estateId));

        return await GetCalculatedPropertyValuesInternalAsync(
            ct => pythagorasClient.GetCalculatedPropertyValuesForEstateAsync(estateId, request, ct),
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IReadOnlyDictionary<PropertyCategoryId, CalculatedPropertyValueDto>> GetCalculatedPropertyValuesInternalAsync(Func<CancellationToken, Task<IReadOnlyDictionary<int, CalculatedPropertyValueDto>>> fetchAsync, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(fetchAsync);

        IReadOnlyDictionary<int, CalculatedPropertyValueDto> rawValues = await fetchAsync(cancellationToken).ConfigureAwait(false);

        if (rawValues.Count == 0)
        {
            return new Dictionary<PropertyCategoryId, CalculatedPropertyValueDto>();
        }

        Dictionary<PropertyCategoryId, CalculatedPropertyValueDto> mapped = new(rawValues.Count);
        foreach (KeyValuePair<int, CalculatedPropertyValueDto> entry in rawValues)
        {
            PropertyCategoryId categoryId = (PropertyCategoryId)entry.Key;
            mapped[categoryId] = entry.Value;
        }

        return mapped;
    }

    private sealed class WorkspaceStatsAccumulator
    {
        public HashSet<int> FloorIds { get; } = [];
        public int RoomCount { get; set; }
    }

    #region Private Helper Methods

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
}
