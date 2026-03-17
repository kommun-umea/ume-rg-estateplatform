namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;

/// <summary>
/// DTO for the Pythagoras work order category info endpoint.
/// Represents a single node in the flat category tree.
/// </summary>
public sealed class WorkOrderCategoryInfoDto : IPythagorasDto
{
    public int Id { get; init; }
    public string? Name { get; init; }
    public int? ParentId { get; init; }
    public List<int>? WorkOrderTypeIds { get; init; }
}
