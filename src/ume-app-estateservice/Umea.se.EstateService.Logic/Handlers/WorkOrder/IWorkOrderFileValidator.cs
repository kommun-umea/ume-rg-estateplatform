namespace Umea.se.EstateService.Logic.Handlers.WorkOrder;

public interface IWorkOrderFileValidator
{
    Task ValidateAsync(IReadOnlyList<WorkOrderFileUpload> files, CancellationToken cancellationToken = default);
}
