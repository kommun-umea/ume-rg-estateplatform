using Umea.se.EstateService.Shared.Search;

namespace Umea.se.EstateService.Shared.Data.Entities;

public class FavoriteEntity
{
    public int Id { get; set; }
    public string UserEmail { get; set; } = null!;
    public NodeType NodeType { get; set; }
    public int NodeId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
