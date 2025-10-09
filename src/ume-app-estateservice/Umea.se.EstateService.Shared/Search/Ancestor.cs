namespace Umea.se.EstateService.Shared.Search;

public sealed class Ancestor
{
    public NodeType Type { get; set; }
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string? PopularName { get; set; }
}
