namespace Umea.se.EstateService.Shared.Models;

public sealed class BuildingImageResult : IDisposable, IAsyncDisposable
{
    private readonly IAsyncDisposable _lifetime;
    private bool _disposed;

    public BuildingImageResult(
        Stream content,
        string? contentType,
        string? fileName,
        long? contentLength,
        int imageId,
        IAsyncDisposable lifetime)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
        ContentType = contentType;
        FileName = fileName;
        ContentLength = contentLength;
        ImageId = imageId;
        _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
    }

    public Stream Content { get; }
    public string? ContentType { get; }
    public string? FileName { get; }
    public long? ContentLength { get; }
    public int ImageId { get; }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _lifetime.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _disposed = true;
        return _lifetime.DisposeAsync();
    }
}
