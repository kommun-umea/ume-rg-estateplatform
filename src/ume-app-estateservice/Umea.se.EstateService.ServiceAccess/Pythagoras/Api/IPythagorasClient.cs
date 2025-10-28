using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enum;

namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Api;

public interface IPythagorasClient
{
    Task<IReadOnlyList<BuildingInfo>> GetBuildingsAsync(PythagorasQuery<BuildingInfo>? query = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BuildingWorkspace>> GetBuildingWorkspacesAsync(int buildingId, PythagorasQuery<BuildingWorkspace>? query = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BuildingAscendant>> GetBuildingAscendantsAsync(int buildingId, PythagorasQuery<BuildingAscendant>? query = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Floor>> GetBuildingFloorsAsync(int buildingId, PythagorasQuery<Floor>? query = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BuildingWorkspace>> GetFloorWorkspacesAsync(int floorId, PythagorasQuery<BuildingWorkspace>? query = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Workspace>> GetWorkspacesAsync(PythagorasQuery<Workspace>? query = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BuildingWorkspace>> GetWorkspaceInfoAsync(PythagorasQuery<BuildingWorkspace>? query = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NavigationFolder>> GetNavigationFoldersAsync(PythagorasQuery<NavigationFolder>? query = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Floor>> GetFloorsAsync(PythagorasQuery<Floor>? query = null, CancellationToken cancellationToken = default);
    Task<HttpResponseMessage> GetFloorBlueprintAsync(int floorId, BlueprintFormat format, bool includeWorkspaceTexts, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<int, CalculatedPropertyValueDto>> GetBuildingCalculatedPropertyValuesAsync(int buildingId, CalculatedPropertyValueRequest? request = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<int, CalculatedPropertyValueDto>> GetCalculatedPropertyValuesForEstateAsync(int estateId, CalculatedPropertyValueRequest? request = null, CancellationToken cancellationToken = default);
    Task<UiListDataResponse<BuildingInfo>> PostBuildingUiListDataAsync(BuildingUiListDataRequest request, CancellationToken cancellationToken = default);
    Task<UiListDataResponse<NavigationFolder>> PostNavigationFolderUiListDataAsync(NavigationFolderUiListDataRequest request, CancellationToken cancellationToken = default);
}
