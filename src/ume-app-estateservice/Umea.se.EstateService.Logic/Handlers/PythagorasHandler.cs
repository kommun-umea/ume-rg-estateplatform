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
        (int)PropertyCategoryId.YearOfConstruction,
        (int)PropertyCategoryId.NoticeBoardText,
        (int)PropertyCategoryId.NoticeBoardStartDate,
        (int)PropertyCategoryId.NoticeBoardEndDate,
        (int)PropertyCategoryId.YearOfConstruction
    ]);

    public async Task<IReadOnlyList<BuildingInfoModel>> GetBuildingsAsync(PythagorasQuery<BuildingInfo>? query = null, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<BuildingInfo> payload = await pythagorasClient.GetBuildingsAsync(query, cancellationToken).ConfigureAwait(false);
        return PythagorasBuildingInfoMapper.ToModel(payload);
    }

    public async Task<IReadOnlyList<BuildingInfoModel>> GetBuildingsWithPropertiesAsync(
        IReadOnlyCollection<int>? buildingIds = null,
        IReadOnlyCollection<int>? propertyIds = null,
        int? navigationId = null,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<int> effectivePropertyIds;
        if (propertyIds?.Any() == true)
        {
            if (propertyIds.Any(id => id <= 0))
            {
                throw new ArgumentOutOfRangeException(nameof(propertyIds), "Property ids must be positive.");
            }

            effectivePropertyIds = [.. propertyIds];
        }
        else
        {
            effectivePropertyIds = _buildingExtendedPropertyIds;
        }

        IReadOnlyCollection<int>? validatedBuildingIds = null;
        if (buildingIds?.Any() == true)
        {
            if (buildingIds.Any(id => id <= 0))
            {
                throw new ArgumentOutOfRangeException(nameof(buildingIds), "Building ids must be positive.");
            }

            validatedBuildingIds = [.. buildingIds];
        }

        BuildingUiListDataRequest request = new()
        {
            BuildingIds = validatedBuildingIds,
            PropertyIds = effectivePropertyIds,
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
        if (buildingId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(buildingId), "Building id must be positive.");
        }

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

    public async Task<IReadOnlyList<BuildingInfoModel>> GetBuildingInfoAsync(PythagorasQuery<BuildingInfo>? query = null, int? navigationFolderId = null, CancellationToken cancellationToken = default)
    {
        query ??= new PythagorasQuery<BuildingInfo>();

        if (navigationFolderId is int folderId)
        {
            if (folderId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(navigationFolderId), "Navigation folder id must be positive when supplied.");
            }

            query = query
                .WithQueryParameter("navigationId", NavigationType.UmeaKommun)
                .WithQueryParameter("navigationFolderId", folderId);
        }

        IReadOnlyList<BuildingInfo> payload = await pythagorasClient.GetBuildingsAsync(query, cancellationToken).ConfigureAwait(false);
        return PythagorasBuildingInfoMapper.ToModel(payload);
    }

    public async Task<IReadOnlyList<BuildingRoomModel>> GetBuildingWorkspacesAsync(int buildingId, PythagorasQuery<BuildingWorkspace>? query = null, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<BuildingWorkspace> payload = await pythagorasClient.GetBuildingWorkspacesAsync(buildingId, query, cancellationToken).ConfigureAwait(false);
        return PythagorasWorkspaceMapper.ToModel(payload);
    }

    public async Task<IReadOnlyList<BuildingAscendantModel>> GetBuildingAscendantsAsync(
        int buildingId,
        CancellationToken cancellationToken = default)
    {
        PythagorasQuery<BuildingAscendant> query = new PythagorasQuery<BuildingAscendant>()
            .WithQueryParameter("navigationId", 2)
            .WithQueryParameter("includeSelf", false);

        IReadOnlyList<BuildingAscendant> payload = await pythagorasClient
            .GetBuildingAscendantsAsync(buildingId, query, cancellationToken)
            .ConfigureAwait(false);

        return PythagorasBuildingAscendantMapper.ToModel(payload);
    }

    public async Task<IReadOnlyList<FloorInfoModel>> GetBuildingFloorsAsync(
        int buildingId,
        PythagorasQuery<Floor>? floorQuery = null,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Floor> floors = await GetBuildingFloorsInternalAsync(buildingId, floorQuery, cancellationToken).ConfigureAwait(false);

        return floors.Count == 0
            ? []
            : PythagorasFloorInfoMapper.ToModel(floors);
    }

    public async Task<IReadOnlyList<FloorInfoModel>> GetBuildingFloorsWithRoomsAsync(
        int buildingId,
        PythagorasQuery<Floor>? floorQuery = null,
        PythagorasQuery<BuildingWorkspace>? workspaceQuery = null,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Floor> floors = await GetBuildingFloorsInternalAsync(buildingId, floorQuery, cancellationToken).ConfigureAwait(false);

        if (floors.Count == 0)
        {
            return [];
        }

        List<FloorInfoModel> result = new(floors.Count);
        foreach (Floor floor in floors)
        {
            if (floor.Id <= 0)
            {
                result.Add(PythagorasFloorInfoMapper.ToModel(floor, []));
                continue;
            }

            IReadOnlyList<BuildingWorkspace> workspaceDtos = await pythagorasClient
                .GetFloorWorkspacesAsync(floor.Id, workspaceQuery, cancellationToken)
                .ConfigureAwait(false);

            IReadOnlyList<BuildingRoomModel> rooms = PythagorasWorkspaceMapper.ToModel(workspaceDtos);
            FloorInfoModel model = PythagorasFloorInfoMapper.ToModel(floor, rooms);
            result.Add(model);
        }

        return result;
    }

    private async Task<IReadOnlyList<Floor>> GetBuildingFloorsInternalAsync(
        int buildingId,
        PythagorasQuery<Floor>? floorQuery,
        CancellationToken cancellationToken)
    {
        if (buildingId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(buildingId), "Building id must be positive.");
        }

        return await pythagorasClient
            .GetBuildingFloorsAsync(buildingId, floorQuery, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<RoomModel>> GetRoomsAsync(PythagorasQuery<Workspace>? query = null, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Workspace> payload = await pythagorasClient.GetWorkspacesAsync(query, cancellationToken).ConfigureAwait(false);
        return PythagorasWorkspaceMapper.ToModel(payload);
    }

    public async Task<IReadOnlyList<EstateModel>> GetEstatesAsync(PythagorasQuery<NavigationFolder>? query = null, CancellationToken cancellationToken = default)
    {
        query ??= new PythagorasQuery<NavigationFolder>();
        query = query
            .Where(f => f.TypeId, NavigationFolderType.Estate)
            .WithQueryParameter("navigationId", NavigationType.UmeaKommun);

        IReadOnlyList<NavigationFolder> payload = await pythagorasClient
            .GetNavigationFoldersAsync(query, cancellationToken)
            .ConfigureAwait(false);

        return PythagorasEstateMapper.ToModel(payload);
    }

    public async Task<IReadOnlyList<EstateModel>> GetEstatesWithPropertiesAsync(
        IReadOnlyCollection<int>? estateIds = null,
        IReadOnlyCollection<int>? propertyIds = null,
        int? navigationId = null,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<int> effectivePropertyIds;
        if (propertyIds?.Any() == true)
        {
            if (propertyIds.Any(id => id <= 0))
            {
                throw new ArgumentOutOfRangeException(nameof(propertyIds), "Property ids must be positive.");
            }

            effectivePropertyIds = [.. propertyIds];
        }
        else
        {
            effectivePropertyIds = _estateCalculatedPropertyIds;
        }

        IReadOnlyCollection<int>? validatedEstateIds = null;
        if (estateIds?.Any() == true)
        {
            if (estateIds.Any(id => id <= 0))
            {
                throw new ArgumentOutOfRangeException(nameof(estateIds), "Estate ids must be positive.");
            }

            validatedEstateIds = [.. estateIds];
        }

        NavigationFolderUiListDataRequest request = new()
        {
            NavigationFolderIds = validatedEstateIds,
            PropertyIds = effectivePropertyIds,
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
        if (estateId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(estateId), "Estate id must be positive.");
        }

        PythagorasQuery<NavigationFolder> query = new PythagorasQuery<NavigationFolder>()
            .Where(folder => folder.Id, estateId)
            .Where(folder => folder.TypeId, NavigationFolderType.Estate)
            .WithQueryParameter("navigationId", NavigationType.UmeaKommun);

        query = query.WithQueryParameter("includeAscendantBuildings", true);

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
        if (buildingId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(buildingId), "Building id must be positive.");
        }

        return await GetCalculatedPropertyValuesInternalAsync(
            ct => pythagorasClient.GetBuildingCalculatedPropertyValuesAsync(buildingId, request, ct),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyDictionary<PropertyCategoryId, CalculatedPropertyValueDto>> GetEstateCalculatedPropertyValuesInternalAsync(int estateId, CalculatedPropertyValueRequest? request = null, CancellationToken cancellationToken = default)
    {
        if (estateId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(estateId), "Estate id must be positive.");
        }

        return await GetCalculatedPropertyValuesInternalAsync(
            ct => pythagorasClient.GetCalculatedPropertyValuesForEstateAsync(estateId, request, ct),
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IReadOnlyDictionary<PropertyCategoryId, CalculatedPropertyValueDto>> GetCalculatedPropertyValuesInternalAsync(
        Func<CancellationToken, Task<IReadOnlyDictionary<int, CalculatedPropertyValueDto>>> fetchAsync,
        CancellationToken cancellationToken)
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
}
