namespace Umea.se.EstateService.Shared.Search;

public interface IFavoriteable
{
    int Id { get; }
    NodeType FavoriteNodeType { get; }
    bool? IsFavorite { get; set; }
}
