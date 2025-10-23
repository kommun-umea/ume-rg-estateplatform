using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.Logic.Mappers;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enum;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Logic.Handlers;

public class PythagorasHandler(IPythagorasClient pythagorasClient) : IPythagorasHandler
{
    private const string WorkspacesEndpoint = "rest/v1/workspace/info";
    private const string BuildingsInfoEndpoint = "rest/v1/building/info";
    private const string NavigationFoldersEndpoint = "rest/v1/navigationfolder/info";
    private const string FloorsInfoEndpoint = "rest/v1/floor/info";
    private static string BuildingFloorsEndpoint(int buildingId) => $"rest/v1/building/{buildingId}/floor";
    private static string FloorWorkspacesEndpoint(int floorId) => $"rest/v1/floor/{floorId}/workspace/info";

    private static string BuildingWorkspacesEndpoint(int buildingId) => $"rest/v1/building/{buildingId}/workspace/info";

    public async Task<IReadOnlyList<BuildingInfoModel>> GetBuildingsAsync(PythagorasQuery<BuildingInfo>? query = null, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<BuildingInfo> payload = await pythagorasClient.GetAsync(BuildingsInfoEndpoint, query, cancellationToken).ConfigureAwait(false);
        return PythagorasBuildingInfoMapper.ToModel(payload);
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

        IReadOnlyList<BuildingInfo> payload = await pythagorasClient.GetAsync(BuildingsInfoEndpoint, query, cancellationToken).ConfigureAwait(false);
        return PythagorasBuildingInfoMapper.ToModel(payload);
    }

    public async Task<IReadOnlyList<BuildingRoomModel>> GetBuildingWorkspacesAsync(int buildingId, PythagorasQuery<BuildingWorkspace>? query = null, CancellationToken cancellationToken = default)
    {
        if (buildingId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(buildingId), "Building id must be positive.");
        }

        string endpoint = BuildingWorkspacesEndpoint(buildingId);
        IReadOnlyList<BuildingWorkspace> payload = await pythagorasClient.GetAsync(endpoint, query, cancellationToken).ConfigureAwait(false);
        return PythagorasWorkspaceMapper.ToModel(payload);
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
                .GetAsync(FloorWorkspacesEndpoint(floor.Id), workspaceQuery, cancellationToken)
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
            .GetAsync(BuildingFloorsEndpoint(buildingId), floorQuery, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<RoomModel>> GetRoomsAsync(PythagorasQuery<Workspace>? query = null, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Workspace> payload = await pythagorasClient.GetAsync(WorkspacesEndpoint, query, cancellationToken).ConfigureAwait(false);
        return PythagorasWorkspaceMapper.ToModel(payload);
    }

    public async Task<IReadOnlyList<EstateModel>> GetEstatesAsync(PythagorasQuery<NavigationFolder>? query = null, CancellationToken cancellationToken = default)
    {
        query ??= new PythagorasQuery<NavigationFolder>();
        query = query
            .Where(f => f.TypeId, NavigationFolderType.Estate)
            .WithQueryParameter("navigationId", NavigationType.UmeaKommun);

        IReadOnlyList<NavigationFolder> payload = await pythagorasClient
            .GetAsync(NavigationFoldersEndpoint, query, cancellationToken)
            .ConfigureAwait(false);

        return PythagorasEstateMapper.ToModel(payload);
    }

    public async Task<IReadOnlyList<FloorInfoModel>> GetFloorsAsync(PythagorasQuery<Floor>? query = null, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Floor> payload = await pythagorasClient
            .GetAsync(FloorsInfoEndpoint, query, cancellationToken)
            .ConfigureAwait(false);

        return PythagorasFloorInfoMapper.ToModel(payload);
    }
}
