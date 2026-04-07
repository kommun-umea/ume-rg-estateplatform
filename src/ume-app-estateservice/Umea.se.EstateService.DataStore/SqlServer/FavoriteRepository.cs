using Microsoft.EntityFrameworkCore;
using Umea.se.EstateService.Shared.Data;
using Umea.se.EstateService.Shared.Data.Entities;
using Umea.se.EstateService.Shared.Search;

namespace Umea.se.EstateService.DataStore.SqlServer;

// Note: Each method calls SaveChangesAsync directly (same pattern as WorkOrderRepository).
// If multi-operation transactions are needed later, consider introducing a Unit of Work abstraction.
public class FavoriteRepository(EstateDbContext dbContext) : IFavoriteRepository
{
    public async Task<FavoriteEntity> AddAsync(FavoriteEntity favorite, CancellationToken cancellationToken = default)
    {
        dbContext.Favorites.Add(favorite);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Duplicate (unique constraint) — ignore for idempotent upsert
            dbContext.Entry(favorite).State = EntityState.Detached;
        }
        return favorite;
    }

    public async Task<bool> RemoveAsync(string email, NodeType nodeType, int nodeId, CancellationToken cancellationToken = default)
    {
        int deleted = await dbContext.Favorites
            .Where(f => f.UserEmail == email && f.NodeType == nodeType && f.NodeId == nodeId)
            .ExecuteDeleteAsync(cancellationToken);
        return deleted > 0;
    }

    public async Task<IReadOnlyList<FavoriteEntity>> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await dbContext.Favorites
            .AsNoTracking()
            .Where(f => f.UserEmail == email)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<HashSet<(NodeType Type, int Id)>> GetFavoriteIdsAsync(string email, CancellationToken cancellationToken = default)
    {
        var pairs = await dbContext.Favorites
            .AsNoTracking()
            .Where(f => f.UserEmail == email)
            .Select(f => new { f.NodeType, f.NodeId })
            .ToListAsync(cancellationToken);
        return [.. pairs.Select(p => (p.NodeType, p.NodeId))];
    }
}
