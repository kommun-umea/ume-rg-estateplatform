namespace Umea.se.EstateService.Shared.Models;

/// <summary>
/// Provides common metadata handling plus sync/async disposal for streamed content results.
/// </summary>
public abstract class DisposableStreamResult : IStreamResourceResult
{
    private bool _disposed;

    protected DisposableStreamResult(Stream content, string? contentType, string? fileName, long? contentLength)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
        ContentType = contentType;
        FileName = fileName;
        ContentLength = contentLength;
    }

    protected Stream Content { get; }
    public string? ContentType { get; }
    public string? FileName { get; }
    public long? ContentLength { get; }

    public Stream OpenContentStream()
    {
        ThrowIfDisposed();
        return Content;
    }

    protected void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, GetType());

    protected virtual void DisposeManagedResources()
    {
    }

    protected virtual ValueTask DisposeManagedResourcesAsync() => ValueTask.CompletedTask;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Content.Dispose();
        DisposeManagedResources();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (Content is IAsyncDisposable asyncContent)
        {
            await asyncContent.DisposeAsync().ConfigureAwait(false);
        }
        else
        {
            Content.Dispose();
        }

        await DisposeManagedResourcesAsync().ConfigureAwait(false);
    }
}
