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

    public async Task<IReadOnlyList<BuildingWorkspaceModel>> GetBuildingWorkspacesAsync(int buildingId, PythagorasQuery<BuildingWorkspace>? query = null, CancellationToken cancellationToken = default)
    {
        if (buildingId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(buildingId), "Building id must be positive.");
        }

        string endpoint = BuildingWorkspacesEndpoint(buildingId);
        IReadOnlyList<BuildingWorkspace> payload = await pythagorasClient.GetAsync(endpoint, query, cancellationToken).ConfigureAwait(false);
        return PythagorasWorkspaceMapper.ToModel(payload);
    }

    public async Task<IReadOnlyList<WorkspaceModel>> GetWorkspacesAsync(PythagorasQuery<Workspace>? query = null, CancellationToken cancellationToken = default)
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
}
