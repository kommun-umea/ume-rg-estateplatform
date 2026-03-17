using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Umea.se.EstateService.Shared.Infrastructure;

namespace Umea.se.EstateService.ServiceAccess.FileStorage;

public sealed class BlobWorkOrderFileStorage(BlobContainerClient container) : IWorkOrderFileStorage
{
    public async Task SaveAsync(string relativePath, Stream content, CancellationToken cancellationToken = default)
    {
        BlobClient blob = container.GetBlobClient(NormalizePath(relativePath));
        await blob.UploadAsync(content, overwrite: true, cancellationToken);
    }

    public async Task<byte[]> ReadAllBytesAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        BlobClient blob = container.GetBlobClient(NormalizePath(relativePath));
        BlobDownloadResult result = await blob.DownloadContentAsync(cancellationToken);
        return result.Content.ToArray();
    }

    public async Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        BlobClient blob = container.GetBlobClient(NormalizePath(relativePath));
        return await blob.OpenReadAsync(cancellationToken: cancellationToken);
    }

    public async Task<bool> ExistsAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        BlobClient blob = container.GetBlobClient(NormalizePath(relativePath));
        return await blob.ExistsAsync(cancellationToken);
    }

    public async Task DeleteWorkOrderFilesAsync(Guid workOrderUid, CancellationToken cancellationToken = default)
    {
        string prefix = $"{workOrderUid}/";
        await foreach (BlobItem item in container.GetBlobsAsync(BlobTraits.None, BlobStates.None, prefix, cancellationToken))
        {
            await container.DeleteBlobIfExistsAsync(item.Name, cancellationToken: cancellationToken);
        }
    }

    private static string NormalizePath(string relativePath) => relativePath.Replace('\\', '/');
}
