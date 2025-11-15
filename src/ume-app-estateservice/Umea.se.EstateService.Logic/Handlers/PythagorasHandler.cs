using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.Logic.Mappers;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api.Request;
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

    public async Task<IReadOnlyList<BuildingInfoModel>> GetBuildingsAsync(int[]? buildingIds = null, int? estateId = null, BuildingIncludeOptions includeOptions = BuildingIncludeOptions.None, QueryArgs? queryArgs = null, CancellationToken cancellationToken = default)
    {
        // If extended properties requested, use UiListData endpoint for efficiency
        if (includeOptions.HasFlag(BuildingIncludeOptions.ExtendedProperties))
        {
            BuildingUiListDataRequest request = new()
            {
                BuildingIds = ValidateAndCloneIds(buildingIds, nameof(buildingIds)),
                PropertyIds = _buildingExtendedPropertyIds,
                NavigationId = NavigationType.UmeaKommun,
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

        // Otherwise use standard endpoint
        PythagorasQuery<BuildingInfo> query = new PythagorasQuery<BuildingInfo>()
            .WithQueryParameter("navigationId", NavigationType.UmeaKommun);

        if (buildingIds is { Length: > 0 })
        {
            query = query.WithIds(buildingIds);
        }

        if (estateId.HasValue)
        {
            query = query.WithQueryParameter("navigationFolderId", estateId.Value);
        }

        query = ApplyQueryArgs(query, queryArgs);

        IReadOnlyList<BuildingInfo> payload = await pythagorasClient.GetBuildingsAsync(query, cancellationToken).ConfigureAwait(false);
        return PythagorasBuildingInfoMapper.ToModel(payload);
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

        BuildingInfoModel building = PythagorasBuildingInfoMapper.ToModel(payload[0], extendedProperties);

        if (includeOptions.HasFlag(BuildingIncludeOptions.Ascendants))
        {
            try
            {
                IReadOnlyList<BuildingAscendantModel> ascendants = await GetBuildingAscendantsAsync(buildingId, cancellationToken).ConfigureAwait(false);

                if (ascendants.Count > 0)
                {
                    foreach (BuildingAscendantModel ascendant in ascendants)
                    {
                        switch (ascendant.Type)
                        {
                            case BuildingAscendantType.Estate:
                                building.Estate ??= ascendant;
                                break;
                            case BuildingAscendantType.Area:
                                building.Region ??= ascendant;
                                break;
                            case BuildingAscendantType.Organization:
                                building.Organization ??= ascendant;
                                break;
                        }
                    }
                }
            }
            catch
            {
                // Silently ignore ascendant loading errors - building data is still valid
            }
        }

        return building;
    }

    public async Task<IReadOnlyList<RoomModel>> GetBuildingWorkspacesAsync(int buildingId, int? floorId = null, QueryArgs? queryArgs = null, CancellationToken cancellationToken = default)
    {
        ValidatePositiveId(buildingId, nameof(buildingId));

        PythagorasQuery<Workspace> query = new PythagorasQuery<Workspace>()
            .Where(workspace => workspace.BuildingId, buildingId);

        if (floorId.HasValue)
        {
            query = query.Where(workspace => workspace.FloorId, floorId.Value);
        }

        query = ApplyQueryArgs(query, queryArgs);

        IReadOnlyList<Workspace> payload = await pythagorasClient
            .GetWorkspacesAsync(query, cancellationToken)
            .ConfigureAwait(false);

        return PythagorasWorkspaceMapper.ToModel(payload);
    }

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

    public async Task<IReadOnlyList<FloorInfoModel>> GetBuildingFloorsAsync(int buildingId, bool includeRooms = false, QueryArgs? floorsQueryArgs = null, QueryArgs? roomsQueryArgs = null, CancellationToken cancellationToken = default)
    {
        ValidatePositiveId(buildingId, nameof(buildingId));

        PythagorasQuery<Floor> floorQuery = new();
        floorQuery = ApplyQueryArgs(floorQuery, floorsQueryArgs);

        IReadOnlyList<Floor> floors = await GetBuildingFloorsInternalAsync(buildingId, floorQuery, cancellationToken).ConfigureAwait(false);

        if (floors.Count == 0)
        {
            return [];
        }

        if (!includeRooms)
        {
            return PythagorasFloorInfoMapper.ToModel(floors);
        }

        IEnumerable<Task<FloorInfoModel>> floorTasks = floors.Select(async floor =>
        {
            if (floor.Id <= 0)
            {
                return PythagorasFloorInfoMapper.ToModel(floor, []);
            }

            PythagorasQuery<Workspace> workspaceQuery = new PythagorasQuery<Workspace>()
                .Where(workspace => workspace.FloorId, floor.Id);

            workspaceQuery = ApplyQueryArgs(workspaceQuery, roomsQueryArgs);

            IReadOnlyList<Workspace> workspaceDtos = await pythagorasClient
                .GetWorkspacesAsync(workspaceQuery, cancellationToken)
                .ConfigureAwait(false);

            IReadOnlyList<RoomModel> rooms = PythagorasWorkspaceMapper.ToModel(workspaceDtos);
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

    public async Task<IReadOnlyList<RoomModel>> GetRoomsAsync(int[]? roomIds = null, int? buildingId = null, int? floorId = null, QueryArgs? queryArgs = null, CancellationToken cancellationToken = default)
    {
        PythagorasQuery<Workspace>? query = null;

        if (roomIds is { Length: > 0 })
        {
            query = new PythagorasQuery<Workspace>().WithIds(roomIds);
        }
        else
        {
            if (buildingId.HasValue)
            {
                query ??= new PythagorasQuery<Workspace>();
                query = query.Where(workspace => workspace.BuildingId, buildingId.Value);
            }

            if (floorId.HasValue)
            {
                query ??= new PythagorasQuery<Workspace>();
                query = query.Where(workspace => workspace.FloorId, floorId.Value);
            }

            if (queryArgs is not null)
            {
                query ??= new PythagorasQuery<Workspace>();
                query = ApplyQueryArgs(query, queryArgs);
            }
        }

        IReadOnlyList<Workspace> payload = await pythagorasClient.GetWorkspacesAsync(query, cancellationToken).ConfigureAwait(false);
        return PythagorasWorkspaceMapper.ToModel(payload);
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
        PythagorasQuery<NavigationFolder> query = new PythagorasQuery<NavigationFolder>()
            .Where(f => f.TypeId, NavigationFolderType.Estate)
            .WithQueryParameter("navigationId", NavigationType.UmeaKommun)
            .WithQueryParameter("includeAscendantBuildings", includeBuildings);

        query = ApplyQueryArgs(query, queryArgs);

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
    public async Task<IReadOnlyList<FloorInfoModel>> GetFloorsAsync(int[]? floorIds = null, QueryArgs? queryArgs = null, CancellationToken cancellationToken = default)
    {
        PythagorasQuery<Floor> query = new();

        if (floorIds is { Length: > 0 })
        {
            query = query.WithQueryParameterValues("floorIds[]", floorIds);
        }

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
}
