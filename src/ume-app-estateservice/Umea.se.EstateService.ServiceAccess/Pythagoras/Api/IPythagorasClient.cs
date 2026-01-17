using Umea.se.EstateService.ServiceAccess.Common;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enum;

namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Api;

public interface IPythagorasClient
{
    Task<IReadOnlyList<BuildingInfo>> GetBuildingsAsync(PythagorasQuery<BuildingInfo>? query = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BuildingAscendant>> GetBuildingAscendantsAsync(int buildingId, PythagorasQuery<BuildingAscendant>? query = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Floor>> GetBuildingFloorsAsync(int buildingId, PythagorasQuery<Floor>? query = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Workspace>> GetWorkspacesAsync(PythagorasQuery<Workspace>? query = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NavigationFolder>> GetNavigationFoldersAsync(PythagorasQuery<NavigationFolder>? query = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Floor>> GetFloorsAsync(PythagorasQuery<Floor>? query = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BusinessType>> GetBusinessTypesAsync(PythagorasQuery<BusinessType>? query = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<int, CalculatedPropertyValueDto>> GetBuildingCalculatedPropertyValuesAsync(int buildingId, CalculatedPropertyValueRequest? request = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<int, CalculatedPropertyValueDto>> GetCalculatedPropertyValuesForEstateAsync(int estateId, CalculatedPropertyValueRequest? request = null, CancellationToken cancellationToken = default);
    Task<UiListDataResponse<BuildingInfo>> PostBuildingUiListDataAsync(BuildingUiListDataRequest request, CancellationToken cancellationToken = default);
    Task<UiListDataResponse<NavigationFolder>> PostNavigationFolderUiListDataAsync(NavigationFolderUiListDataRequest request, CancellationToken cancellationToken = default);
    Task<BinaryResourceResult?> GetFloorBlueprintAsync(int floorId, BlueprintFormat format, IDictionary<int, IReadOnlyList<string>>? workspaceTexts, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GalleryImageFile>> GetBuildingGalleryImagesAsync(int buildingId, CancellationToken cancellationToken = default);
    Task<BinaryResourceResult?> GetGalleryImageDataAsync(int imageId, GalleryImageVariant variant, CancellationToken cancellationToken = default);
}
