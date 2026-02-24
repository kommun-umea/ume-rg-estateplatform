using Umea.se.EstateService.Logic.Models;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enum;

namespace Umea.se.EstateService.Logic.Handlers.Blueprint;

public interface IFloorBlueprintService
{
    Task<FloorBlueprint> GetBlueprintAsync(int floorId, BlueprintFormat format, bool includeWorkspaceTexts = true, CancellationToken cancellationToken = default);
}
