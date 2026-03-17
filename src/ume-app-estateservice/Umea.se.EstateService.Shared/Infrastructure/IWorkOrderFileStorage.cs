namespace Umea.se.EstateService.Shared.Infrastructure;

public interface IWorkOrderFileStorage
{
    Task SaveAsync(string relativePath, Stream content, CancellationToken cancellationToken = default);
    Task<byte[]> ReadAllBytesAsync(string relativePath, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string relativePath, CancellationToken cancellationToken = default);
    Task DeleteWorkOrderFilesAsync(Guid workOrderUid, CancellationToken cancellationToken = default);
}
