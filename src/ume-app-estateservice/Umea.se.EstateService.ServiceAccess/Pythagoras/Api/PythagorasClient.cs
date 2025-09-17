using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Api;

public interface IPythagorasClient
{
    Task<IReadOnlyList<TDto>> GetAsync<TDto>(Action<PythagorasQuery<TDto>>? query = null,CancellationToken cancellationToken = default) where TDto : class;
    IAsyncEnumerable<TDto> GetPaginatedAsync<TDto>(Action<PythagorasQuery<TDto>>? query, int pageSize, CancellationToken cancellationToken = default) where TDto : class;
}

public sealed class PythagorasClient(IHttpClientFactory httpClientFactory) : IPythagorasClient
{
    private static readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public Task<IReadOnlyList<TDto>> GetAsync<TDto>(Action<PythagorasQuery<TDto>>? configure = null, CancellationToken cancellationToken = default) where TDto : class
    {
        return QueryAsync(configure, cancellationToken);
    }

    public async IAsyncEnumerable<TDto> GetPaginatedAsync<TDto>(Action<PythagorasQuery<TDto>>? configure, int pageSize, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TDto : class
    {
        if (pageSize <= 0)
        {
            throw new ArgumentException("Page size must be > 0.", nameof(pageSize));
        }

        int pageNumber = 1;
        IReadOnlyList<TDto> page;
        do
        {
            page = await QueryAsync(configure, cancellationToken, builder => builder.Page(pageNumber, pageSize)).ConfigureAwait(false);

            foreach (TDto item in page)
            {
                yield return item;
            }

            pageNumber++;
        }
        while (page.Count == pageSize);
    }

    private async Task<IReadOnlyList<TDto>> QueryAsync<TDto>(Action<PythagorasQuery<TDto>>? configure, CancellationToken cancellationToken, Action<PythagorasQuery<TDto>>? afterConfigure = null)
        where TDto : class
    {
        PythagorasQuery<TDto> builder = new();
        configure?.Invoke(builder);
        afterConfigure?.Invoke(builder);

        string requestPath = PythagorasEndpointResolver.Resolve(typeof(TDto));
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
}
