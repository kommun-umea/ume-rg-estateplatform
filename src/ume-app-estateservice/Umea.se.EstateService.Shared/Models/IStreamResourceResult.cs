namespace Umea.se.EstateService.Shared.Models;

public interface IStreamResourceResult : IDisposable, IAsyncDisposable
{
    string? ContentType { get; }
    string? FileName { get; }
    long? ContentLength { get; }
    Stream OpenContentStream();
}
