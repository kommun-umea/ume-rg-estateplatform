using Umea.se.EstateService.Shared.Search;

namespace Umea.se.EstateService.Logic.Handlers.Favorite;

public static class FavoriteHandlerExtensions
{
    public static async Task StampFavoritesAsync<T>(
        this IFavoriteHandler favoriteHandler,
        string email,
        IEnumerable<T> items,
        CancellationToken cancellationToken = default) where T : IFavoriteable
    {
        HashSet<(NodeType Type, int Id)> favoriteIds = await favoriteHandler.GetFavoriteIdsAsync(email, cancellationToken);
        foreach (T item in items)
        {
            item.IsFavorite = favoriteIds.Contains((item.FavoriteNodeType, item.Id));
        }
    }

    public static async Task StampFavoriteAsync(
        this IFavoriteHandler favoriteHandler,
        string email,
        IFavoriteable item,
        CancellationToken cancellationToken = default)
    {
        HashSet<(NodeType Type, int Id)> favoriteIds = await favoriteHandler.GetFavoriteIdsAsync(email, cancellationToken);
        item.IsFavorite = favoriteIds.Contains((item.FavoriteNodeType, item.Id));
    }
}
