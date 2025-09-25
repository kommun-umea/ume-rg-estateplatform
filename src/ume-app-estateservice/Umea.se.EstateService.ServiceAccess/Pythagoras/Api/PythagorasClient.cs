using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Api;

public sealed class PythagorasClient(IHttpClientFactory httpClientFactory) : IPythagorasClient
{
    private static readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public Task<IReadOnlyList<TDto>> GetAsync<TDto>(string endpoint, PythagorasQuery<TDto>? query, CancellationToken cancellationToken = default) where TDto : class
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        query ??= new PythagorasQuery<TDto>();
        return QueryAsync(endpoint, query, cancellationToken);
    }

    public async IAsyncEnumerable<TDto> GetPaginatedAsync<TDto>(string endpoint, PythagorasQuery<TDto>? query, int pageSize = 50, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TDto : class
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        if (pageSize <= 0)
        {
            throw new ArgumentException("Page size must be > 0.", nameof(pageSize));
        }

        PythagorasQuery<TDto> baseQuery = query ?? new PythagorasQuery<TDto>();
        int pageNumber = 1;

        while (true)
        {
            PythagorasQuery<TDto> pageQuery = baseQuery.Page(pageNumber, pageSize);

            IReadOnlyList<TDto> page = await QueryAsync(endpoint, pageQuery, cancellationToken).ConfigureAwait(false);
            if (page.Count == 0)
            {
                yield break;
            }

            foreach (TDto item in page)
            {
                yield return item;
            }

            if (page.Count < pageSize)
            {
                yield break;
            }

            pageNumber++;
        }
    }

    private async Task<IReadOnlyList<TDto>> QueryAsync<TDto>(string endpoint, PythagorasQuery<TDto> query, CancellationToken cancellationToken)
        where TDto : class
    {
        string requestPath = NormalizeEndpoint(endpoint);
        string queryString = query.BuildAsQueryString();
        string requestUri = BuildRequestUri(requestPath, queryString);

        HttpClient client = httpClientFactory.CreateClient(HttpClientNames.Pythagoras);
        using HttpResponseMessage response = await client.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        using Stream contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
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

        if (Uri.TryCreate(trimmed, UriKind.RelativeOrAbsolute, out Uri? candidate)
            && candidate.IsAbsoluteUri
            && (candidate.Scheme == Uri.UriSchemeHttp || candidate.Scheme == Uri.UriSchemeHttps))
        {
            return candidate.AbsoluteUri;
        }

        string normalized = trimmed.TrimStart('/', '\\');
        if (normalized.Length == 0)
        {
            throw new ArgumentException("Endpoint must contain a path segment.", nameof(endpoint));
        }

        return normalized;
    }
}
