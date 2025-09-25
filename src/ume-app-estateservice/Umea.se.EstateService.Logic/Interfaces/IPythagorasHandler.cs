using Umea.se.EstateService.ServiceAccess.Pythagoras;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Logic.Interfaces;

public interface IPythagorasHandler
{
    Task<IReadOnlyList<BuildingModel>> GetBuildingsAsync(PythagorasQuery<Building>? query = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BuildingInfoModel>> GetBuildingInfoAsync(PythagorasQuery<BuildingInfo>? query = null, int? navigationFolderId = null, CancellationToken cancellationToken = default);

    IAsyncEnumerable<BuildingModel> GetPaginatedBuildingsAsync(PythagorasQuery<Building>? query = null, int pageSize = 50, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BuildingWorkspaceModel>> GetBuildingWorkspacesAsync(int buildingId, PythagorasQuery<BuildingWorkspace>? query = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkspaceModel>> GetWorkspacesAsync(PythagorasQuery<Workspace>? query = null, CancellationToken cancellationToken = default);

    IAsyncEnumerable<WorkspaceModel> GetPaginatedWorkspacesAsync(PythagorasQuery<Workspace>? query = null, int pageSize = 50, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EstateModel>> GetEstatesAsync(PythagorasQuery<NavigationFolder>? query = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BuildingSearchResult>> SearchBuildingsAsync(string searchTerm, int limit = 10, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkspaceSearchResult>> SearchWorkspacesAsync(string searchTerm, int limit = 10, int? buildingId = null, CancellationToken cancellationToken = default);
}
