using Umea.se.EstateService.Shared.Infrastructure;

namespace Umea.se.EstateService.ServiceAccess.FileStorage;

public sealed class LocalWorkOrderFileStorage(ApplicationConfig config) : IWorkOrderFileStorage
{
    private readonly string _basePath = config.WorkOrderProcessing.FileStorage;

    public async Task SaveAsync(string relativePath, Stream content, CancellationToken cancellationToken = default)
    {
        string fullPath = Path.Combine(_basePath, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using FileStream fs = File.Create(fullPath);
        await content.CopyToAsync(fs, cancellationToken);
    }

    public async Task<byte[]> ReadAllBytesAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        string fullPath = Path.Combine(_basePath, relativePath);
        return await File.ReadAllBytesAsync(fullPath, cancellationToken);
    }

    public Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        string fullPath = Path.Combine(_basePath, relativePath);
        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult(stream);
    }

    public Task<bool> ExistsAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        string fullPath = Path.Combine(_basePath, relativePath);
        return Task.FromResult(File.Exists(fullPath));
    }

    public Task DeleteWorkOrderFilesAsync(Guid workOrderUid, CancellationToken cancellationToken = default)
    {
        string dir = Path.Combine(_basePath, workOrderUid.ToString());
        if (Directory.Exists(dir))
        {
            Directory.Delete(dir, recursive: true);
        }

        return Task.CompletedTask;
    }
}
