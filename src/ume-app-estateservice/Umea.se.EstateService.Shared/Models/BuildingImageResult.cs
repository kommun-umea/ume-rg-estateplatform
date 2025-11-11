namespace Umea.se.EstateService.Shared.Models;

public sealed class BuildingImageResult : DisposableStreamResult
{
    private readonly IDisposable _lifetime;
    private readonly IAsyncDisposable? _asyncLifetime;

    public BuildingImageResult(
        Stream content,
        string? contentType,
        string? fileName,
        long? contentLength,
        int imageId,
        IDisposable lifetime,
        IAsyncDisposable? asyncLifetime = null)
        : base(content, contentType, fileName, contentLength)
    {
        ImageId = imageId;
        _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
        _asyncLifetime = asyncLifetime;
    }

    public int ImageId { get; }

    protected override void DisposeManagedResources() => _lifetime.Dispose();

    protected override async ValueTask DisposeManagedResourcesAsync()
    {
        if (_asyncLifetime is not null)
        {
            await _asyncLifetime.DisposeAsync().ConfigureAwait(false);
        }
        else
        {
            _lifetime.Dispose();
        }
    }
}
