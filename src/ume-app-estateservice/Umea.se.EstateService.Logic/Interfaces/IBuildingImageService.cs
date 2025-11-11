using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Logic.Interfaces;

public interface IBuildingImageService
{
    Task<IStreamResourceResult?> GetPrimaryImageAsync(int buildingId, BuildingImageSize size, CancellationToken cancellationToken = default);
}
