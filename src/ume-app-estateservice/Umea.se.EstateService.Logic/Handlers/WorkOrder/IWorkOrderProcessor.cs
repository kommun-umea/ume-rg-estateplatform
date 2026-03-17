namespace Umea.se.EstateService.Logic.Handlers.WorkOrder;

public interface IWorkOrderProcessor
{
    Task ProcessPendingAsync(CancellationToken cancellationToken);
}
