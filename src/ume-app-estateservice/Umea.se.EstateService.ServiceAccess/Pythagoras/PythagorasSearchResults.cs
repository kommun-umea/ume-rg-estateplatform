namespace Umea.se.EstateService.ServiceAccess.Pythagoras;

public sealed class BuildingSearchResult
{
    public int Id { get; init; }

    public Guid? Uid { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? PopularName { get; init; }
}

public sealed class WorkspaceSearchResult
{
    public int Id { get; init; }

    public Guid? Uid { get; init; }

    public int? BuildingId { get; init; }

    public string? BuildingName { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? PopularName { get; init; }
}
