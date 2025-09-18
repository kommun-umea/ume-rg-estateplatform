using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Api;

public interface IPythagorasClient
{
    Task<IReadOnlyList<TDto>> GetAsync<TDto>(string endpoint, Action<PythagorasQuery<TDto>>? query = null, CancellationToken cancellationToken = default) where TDto : class;
    IAsyncEnumerable<TDto> GetPaginatedAsync<TDto>(string endpoint, Action<PythagorasQuery<TDto>>? query = null, int pageSize = 50, CancellationToken cancellationToken = default) where TDto : class;
}

public sealed class PythagorasClient(IHttpClientFactory httpClientFactory) : IPythagorasClient
{
    private static readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public Task<IReadOnlyList<TDto>> GetAsync<TDto>(string endpoint, Action<PythagorasQuery<TDto>>? query = null, CancellationToken cancellationToken = default) where TDto : class
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        return QueryAsync(endpoint, query, cancellationToken);
    }

    public async IAsyncEnumerable<TDto> GetPaginatedAsync<TDto>(string endpoint, Action<PythagorasQuery<TDto>>? query = null, int pageSize = 50, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TDto : class
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        if (pageSize <= 0)
        {
            throw new ArgumentException("Page size must be > 0.", nameof(pageSize));
        }

        int pageNumber = 1;
        IReadOnlyList<TDto> page;
        do
        {
            page = await QueryAsync(endpoint, query, cancellationToken, builder => builder.Page(pageNumber, pageSize)).ConfigureAwait(false);

            foreach (TDto item in page)
            {
                yield return item;
            }

            pageNumber++;
        }
        while (page.Count == pageSize);
    }

    private async Task<IReadOnlyList<TDto>> QueryAsync<TDto>(string endpoint, Action<PythagorasQuery<TDto>>? configure, CancellationToken cancellationToken, Action<PythagorasQuery<TDto>>? afterConfigure = null)
        where TDto : class
    {
        PythagorasQuery<TDto> builder = new();
        configure?.Invoke(builder);
        afterConfigure?.Invoke(builder);

        string requestPath = NormalizeEndpoint(endpoint);
        string query = builder.BuildAsQueryString();
        string requestUri = BuildRequestUri(requestPath, query);

        HttpClient client = httpClientFactory.CreateClient(HttpClientNames.Pythagoras);
        using HttpResponseMessage response = await client.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using Stream contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        List<TDto>? payload = await JsonSerializer.DeserializeAsync<List<TDto>>(contentStream, _serializerOptions, cancellationToken).ConfigureAwait(false);

        return payload ?? [];
    }

    private static string BuildRequestUri(string path, string query) => string.IsNullOrEmpty(query) ? path : $"{path}?{query}";

    private static string NormalizeEndpoint(string endpoint)
    {
        string trimmed = endpoint.Trim();
        if (trimmed.Length == 0)
        {
            throw new ArgumentException("Endpoint must be non-empty.", nameof(endpoint));
        }

        return trimmed.TrimStart('/');
    }
}
