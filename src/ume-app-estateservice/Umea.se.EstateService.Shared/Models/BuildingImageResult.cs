namespace Umea.se.EstateService.Shared.Models;

public sealed class BuildingImageResult : IDisposable, IAsyncDisposable
{
    private readonly HttpResponseMessage _response;
    private bool _disposed;

    public BuildingImageResult(
        Stream content,
        string? contentType,
        string? fileName,
        long? contentLength,
        int imageId,
        HttpResponseMessage response)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
        ContentType = contentType;
        FileName = fileName;
        ContentLength = contentLength;
        ImageId = imageId;
        _response = response ?? throw new ArgumentNullException(nameof(response));
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

        _response.Dispose();
        _disposed = true;
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
