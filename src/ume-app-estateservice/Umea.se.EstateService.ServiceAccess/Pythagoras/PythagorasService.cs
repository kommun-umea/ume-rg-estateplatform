using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;

namespace Umea.se.EstateService.ServiceAccess.Pythagoras;

public class PythagorasService(IPythagorasClient pythagorasClient)
{
    private const string BuildingsEndpoint = "rest/v1/building";

    public Task<IReadOnlyList<Building>> GetBuildingsAsync(Action<PythagorasQuery<Building>>? query = null, CancellationToken cancellationToken = default)
    {
        return pythagorasClient.GetAsync(BuildingsEndpoint, query, cancellationToken);
    }

    public IAsyncEnumerable<Building> GetPaginatedBuildingsAsync(Action<PythagorasQuery<Building>>? query = null, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        return pythagorasClient.GetPaginatedAsync(BuildingsEndpoint, query, pageSize, cancellationToken);
    }
}
