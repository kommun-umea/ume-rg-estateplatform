using System.Runtime.CompilerServices;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Pythagoras;

namespace Umea.se.EstateService.ServiceAccess.Pythagoras;

public class PythagorasService(IPythagorasClient pythagorasClient)
{
    private const string BuildingsEndpoint = "rest/v1/building";
    private const string WorkspacesEndpoint = "rest/v1/workspace";

    public async Task<IReadOnlyList<BuildingModel>> GetBuildingsAsync(Action<PythagorasQuery<Building>>? query = null, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Building> payload = await pythagorasClient.GetAsync(BuildingsEndpoint, query, cancellationToken).ConfigureAwait(false);
        return PythagorasBuildingMapper.ToDomain(payload);
    }

    public async IAsyncEnumerable<BuildingModel> GetPaginatedBuildingsAsync(Action<PythagorasQuery<Building>>? query = null, int pageSize = 50, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (Building dto in pythagorasClient.GetPaginatedAsync(BuildingsEndpoint, query, pageSize, cancellationToken).ConfigureAwait(false))
        {
            yield return PythagorasBuildingMapper.ToDomain(dto);
        }
    }

    public async Task<IReadOnlyList<BuildingWorkspaceModel>> GetBuildingWorkspacesAsync(int buildingId, Action<PythagorasQuery<BuildingWorkspace>>? query = null, CancellationToken cancellationToken = default)
    {
        if (buildingId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(buildingId), "Building id must be positive.");
        }

        string endpoint = BuildBuildingWorkspacesEndpoint(buildingId);
        IReadOnlyList<BuildingWorkspace> payload = await pythagorasClient.GetAsync(endpoint, query, cancellationToken).ConfigureAwait(false);
        return PythagorasWorkspaceMapper.ToDomain(payload);
    }

    public async Task<IReadOnlyList<WorkspaceModel>> GetWorkspacesAsync(Action<PythagorasQuery<Workspace>>? query = null, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Workspace> payload = await pythagorasClient.GetAsync(WorkspacesEndpoint, query, cancellationToken).ConfigureAwait(false);
        return PythagorasWorkspaceMapper.ToDomain(payload);
    }

    public async IAsyncEnumerable<WorkspaceModel> GetPaginatedWorkspacesAsync(Action<PythagorasQuery<Workspace>>? query = null, int pageSize = 50, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (Workspace dto in pythagorasClient.GetPaginatedAsync(WorkspacesEndpoint, query, pageSize, cancellationToken).ConfigureAwait(false))
        {
            yield return PythagorasWorkspaceMapper.ToDomain(dto);
        }
    }

    private static string BuildBuildingWorkspacesEndpoint(int buildingId) => $"rest/v1/building/{buildingId}/workspace/info";
}
