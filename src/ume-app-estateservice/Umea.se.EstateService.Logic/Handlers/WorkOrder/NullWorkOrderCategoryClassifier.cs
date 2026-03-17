using Umea.se.EstateService.Shared.Data.Entities;

namespace Umea.se.EstateService.Logic.Handlers.WorkOrder;

public class NullWorkOrderCategoryClassifier : IWorkOrderCategoryClassifier
{
    public IReadOnlyList<WorkOrderCategoryNode> GetCategoriesForType(int workOrderTypeId) => [];

    public Task<IReadOnlyList<WorkOrderCategorySuggestion>> ClassifyAsync(
        string description, int workOrderTypeId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<WorkOrderCategorySuggestion>>([]);
}
