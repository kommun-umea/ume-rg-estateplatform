using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using System.Runtime.CompilerServices;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Pythagoras;

namespace Umea.se.EstateService.ServiceAccess.Pythagoras;

public class PythagorasService(IPythagorasClient pythagorasClient)
{
    private const string BuildingsEndpoint = "rest/v1/building";

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
}
