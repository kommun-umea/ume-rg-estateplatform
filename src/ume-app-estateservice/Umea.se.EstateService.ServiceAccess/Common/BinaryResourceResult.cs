using System.Net;
using System.Net.Http.Headers;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.ServiceAccess.Common;

/// <summary>
/// Represents a streamed binary payload plus associated metadata.
/// </summary>
public sealed class BinaryResourceResult(Stream content,
    string? contentType,
    string? fileName,
    long? length,
    string? eTag,
    IReadOnlyDictionary<string, string?> headers,
    HttpResponseMessage? response = null)
    : DisposableStreamResult(content, contentType, fileName, length)
{
    private readonly HttpResponseMessage? _response = response;
    public string? ETag { get; } = eTag;
    public IReadOnlyDictionary<string, string?> Headers { get; } = headers ?? throw new ArgumentNullException(nameof(headers));

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

    protected override void DisposeManagedResources() => _response?.Dispose();

    protected override async ValueTask DisposeManagedResourcesAsync()
    {
        if (_response is IAsyncDisposable asyncResponse)
        {
            await asyncResponse.DisposeAsync().ConfigureAwait(false);
        }
        else
        {
            _response?.Dispose();
        }
    }
}
