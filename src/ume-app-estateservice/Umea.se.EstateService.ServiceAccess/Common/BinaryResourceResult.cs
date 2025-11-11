using System.Net;
using System.Net.Http.Headers;

namespace Umea.se.EstateService.ServiceAccess.Common;

/// <summary>
/// Represents a streamed binary payload plus associated metadata.
/// </summary>
public sealed record BinaryResourceResult(
    Stream Content,
    string? ContentType,
    string? FileName,
    long? Length,
    string? ETag,
    IReadOnlyDictionary<string, string?> Headers) : IDisposable, IAsyncDisposable
{
    private readonly HttpResponseMessage? _response;
    private bool _disposed;

    public BinaryResourceResult(
        Stream content,
        string? contentType,
        string? fileName,
        long? length,
        string? eTag,
        IReadOnlyDictionary<string, string?> headers,
        HttpResponseMessage? response)
        : this(content, contentType, fileName, length, eTag, headers)
    {
        _response = response;
    }

    public static async Task<BinaryResourceResult?> CreateFromResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            response.Dispose();
            return null;
        }

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch
        {
            response.Dispose();
            throw;
        }

        Dictionary<string, string?> headers = new(StringComparer.OrdinalIgnoreCase);

        foreach ((string key, IEnumerable<string> values) in response.Headers)
        {
            headers[key] = values.FirstOrDefault();
        }

        foreach ((string key, IEnumerable<string> values) in response.Content.Headers)
        {
            headers[key] = values.FirstOrDefault();
        }

        MediaTypeHeaderValue? contentType = response.Content.Headers.ContentType;
        ContentDispositionHeaderValue? contentDisposition = response.Content.Headers.ContentDisposition;

        Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

        string? fileName = contentDisposition?.FileNameStar ?? contentDisposition?.FileName;
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            fileName = fileName.Trim('"');
        }

        return new BinaryResourceResult(
            stream,
            contentType?.MediaType,
            fileName,
            response.Content.Headers.ContentLength,
            response.Headers.ETag?.Tag,
            headers,
            response);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Content.Dispose();
        _response?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await Content.DisposeAsync().ConfigureAwait(false);
        _response?.Dispose();
    }
}
