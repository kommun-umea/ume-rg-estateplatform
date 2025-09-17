using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;

namespace Umea.se.EstateService.ServiceAccess.Pythagoras;

public class PythagorasService(IPythagorasClient pythagorasClient)
{
    public Task<IReadOnlyList<Building>> GetBuildingsAsync(Action<PythagorasQuery<Building>>? query = null, CancellationToken cancellationToken = default)
    {
        return pythagorasClient.GetAsync(query, cancellationToken);
    }

    public IAsyncEnumerable<Building> GetPaginatedBuildingsAsync(Action<PythagorasQuery<Building>>? query = null, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        return pythagorasClient.GetPaginatedAsync(query, pageSize, cancellationToken);
    }
}
