namespace Umea.se.EstateService.Shared.Data.Entities;

/// <summary>
/// A single node in the work order category tree.
/// Stored as JSON in the sync metadata table; loaded into DataSnapshot for in-memory lookups.
/// </summary>
public sealed class WorkOrderCategoryNode
{
    public int Id { get; init; }
    public int? ParentId { get; init; }
    public string Name { get; init; } = string.Empty;
    public List<int> WorkOrderTypeIds { get; init; } = [];
}
