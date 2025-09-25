namespace Umea.se.EstateService.Shared.Search;

public sealed class Ancestor
{
    public NodeType Type { get; set; }
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? PopularName { get; set; }
}
