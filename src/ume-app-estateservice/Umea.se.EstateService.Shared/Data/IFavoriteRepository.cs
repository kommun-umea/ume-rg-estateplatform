using Umea.se.EstateService.Shared.Data.Entities;
using Umea.se.EstateService.Shared.Search;

namespace Umea.se.EstateService.Shared.Data;

public interface IFavoriteRepository
{
    Task<FavoriteEntity> AddAsync(FavoriteEntity favorite, CancellationToken cancellationToken = default);
    Task<bool> RemoveAsync(string email, NodeType nodeType, int nodeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FavoriteEntity>> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<HashSet<(NodeType Type, int Id)>> GetFavoriteIdsAsync(string email, CancellationToken cancellationToken = default);
}
