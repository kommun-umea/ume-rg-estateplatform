namespace Umea.se.EstateService.Shared.Data.Entities;

public sealed class WorkOrderCategorySuggestion
{
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public double Confidence { get; init; }
}
