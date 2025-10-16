using System.Runtime.CompilerServices;
using System.Text.Json;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enum;
using Umea.se.Toolkit.ExternalService;

namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Api;

public sealed class PythagorasClient(IHttpClientFactory httpClientFactory)
    : ExternalServiceBase(HttpClientNames.Pythagoras, httpClientFactory), IPythagorasClient
{
    private static readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    protected override string PingUrl => "";

    public Task<IReadOnlyList<TDto>> GetAsync<TDto>(string endpoint, PythagorasQuery<TDto>? query, CancellationToken cancellationToken = default) where TDto : class, IPythagorasDto
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        query ??= new PythagorasQuery<TDto>();
        return QueryAsync(endpoint, query, cancellationToken);
    }

    public async IAsyncEnumerable<TDto> GetPaginatedAsync<TDto>(string endpoint, PythagorasQuery<TDto>? query, int pageSize = 50, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TDto : class, IPythagorasDto
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
        where TDto : class, IPythagorasDto
    {
        string requestPath = NormalizeEndpoint(endpoint);
        string queryString = query.BuildAsQueryString();
        string requestUri = BuildRequestUri(requestPath, queryString);

        using HttpResponseMessage response = await HttpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
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

    public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.RequestUri is null)
        {
            throw new ArgumentException("Request must have RequestUri set.", nameof(request));
        }

        return HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    }

    public Task<HttpResponseMessage> GetFloorBlueprintAsync(int floorId, BlueprintFormat format, bool includeWorkspaceTexts, CancellationToken cancellationToken = default)
    {
        if (floorId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(floorId), "Floor id must be positive.");
        }

        string endpoint = $"rest/v1/floor/{floorId}/gmodel/print/{FormatToSegment(format)}";
        FloorBlueprintRequestPayload payload = FloorBlueprintRequestPayload.CreateDefault();

        HttpRequestMessage request = new(HttpMethod.Post, endpoint)
        {
            Content = BuildBlueprintContent(payload)
        };

        return HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    }

    private static FormUrlEncodedContent BuildBlueprintContent(FloorBlueprintRequestPayload payload)
    {
        string requestJson = JsonSerializer.Serialize(payload, _serializerOptions);
        List<KeyValuePair<string, string>> pairs =
        [
            new("requestObject", requestJson)
        ];

        return new FormUrlEncodedContent(pairs);
    }

    private static string FormatToSegment(BlueprintFormat format) => format switch
    {
        BlueprintFormat.Pdf => "pdf",
        BlueprintFormat.Svg => "svg",
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported blueprint format.")
    };
}
