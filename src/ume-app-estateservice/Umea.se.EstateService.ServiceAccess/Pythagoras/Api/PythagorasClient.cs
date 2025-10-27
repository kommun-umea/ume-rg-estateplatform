using System.Globalization;
using System.Text;
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
        PropertyNameCaseInsensitive = true,
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
    };

    protected override string PingUrl => "";

    public Task<IReadOnlyList<BuildingInfo>> GetBuildingsAsync(PythagorasQuery<BuildingInfo>? query = null, CancellationToken cancellationToken = default)
    {
        query ??= new PythagorasQuery<BuildingInfo>();
        return QueryAsync("rest/v1/building/info", query, cancellationToken);
    }

    public Task<IReadOnlyList<BuildingWorkspace>> GetBuildingWorkspacesAsync(int buildingId, PythagorasQuery<BuildingWorkspace>? query = null, CancellationToken cancellationToken = default)
    {
        if (buildingId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(buildingId), "Building id must be positive.");
        }

        string endpoint = $"rest/v1/building/{buildingId}/workspace/info";
        query ??= new PythagorasQuery<BuildingWorkspace>();
        return QueryAsync(endpoint, query, cancellationToken);
    }

    public Task<IReadOnlyList<BuildingAscendant>> GetBuildingAscendantsAsync(int buildingId, PythagorasQuery<BuildingAscendant>? query = null, CancellationToken cancellationToken = default)
    {
        if (buildingId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(buildingId), "Building id must be positive.");
        }

        string endpoint = $"rest/v1/building/{buildingId}/node/ascendant";
        query ??= new PythagorasQuery<BuildingAscendant>();
        return QueryAsync(endpoint, query, cancellationToken);
    }

    public Task<IReadOnlyList<Floor>> GetBuildingFloorsAsync(int buildingId, PythagorasQuery<Floor>? query = null, CancellationToken cancellationToken = default)
    {
        if (buildingId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(buildingId), "Building id must be positive.");
        }

        string endpoint = $"rest/v1/building/{buildingId}/floor";
        query ??= new PythagorasQuery<Floor>();
        return QueryAsync(endpoint, query, cancellationToken);
    }

    public Task<IReadOnlyList<BuildingWorkspace>> GetFloorWorkspacesAsync(int floorId, PythagorasQuery<BuildingWorkspace>? query = null, CancellationToken cancellationToken = default)
    {
        if (floorId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(floorId), "Floor id must be positive.");
        }

        string endpoint = $"rest/v1/floor/{floorId}/workspace/info";
        query ??= new PythagorasQuery<BuildingWorkspace>();
        return QueryAsync(endpoint, query, cancellationToken);
    }

    public Task<IReadOnlyList<Workspace>> GetWorkspacesAsync(PythagorasQuery<Workspace>? query = null, CancellationToken cancellationToken = default)
    {
        query ??= new PythagorasQuery<Workspace>();
        return QueryAsync("rest/v1/workspace/info", query, cancellationToken);
    }

    public Task<IReadOnlyList<NavigationFolder>> GetNavigationFoldersAsync(PythagorasQuery<NavigationFolder>? query = null, CancellationToken cancellationToken = default)
    {
        query ??= new PythagorasQuery<NavigationFolder>();
        return QueryAsync("rest/v1/navigationfolder/info", query, cancellationToken);
    }

    public Task<IReadOnlyList<Floor>> GetFloorsAsync(PythagorasQuery<Floor>? query = null, CancellationToken cancellationToken = default)
    {
        query ??= new PythagorasQuery<Floor>();
        return QueryAsync("rest/v1/floor/info", query, cancellationToken);
    }

    public Task<IReadOnlyDictionary<int, CalculatedPropertyValueDto>> GetBuildingCalculatedPropertyValuesAsync(int buildingId, CalculatedPropertyValueRequest? request = null, CancellationToken cancellationToken = default)
    {
        if (buildingId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(buildingId), "Building id must be positive.");
        }

        return GetCalculatedPropertyValuesAsync("building", buildingId, request, cancellationToken);
    }

    public Task<IReadOnlyDictionary<int, CalculatedPropertyValueDto>> GetCalculatedPropertyValuesForEstateAsync(int estateId, CalculatedPropertyValueRequest? request = null, CancellationToken cancellationToken = default)
    {
        if (estateId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(estateId), "Estate id must be positive.");
        }

        return GetCalculatedPropertyValuesAsync("navigationfolder", estateId, request, cancellationToken);
    }

    public async Task<UiListDataResponse<BuildingInfo>> PostBuildingUiListDataAsync(
        BuildingUiListDataRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.BuildingIds is { Count: > 0 })
        {
            foreach (int buildingId in request.BuildingIds)
            {
                if (buildingId <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(request), "Building ids must be positive.");
                }
            }
        }

        if (request.PropertyIds is not null)
        {
            foreach (int propertyId in request.PropertyIds)
            {
                if (propertyId <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(request), "Property ids must be positive.");
                }
            }
        }

        string endpoint = "rest/v1/building/info/uilistdata";
        string queryString = BuildUiListDataQuery(request);
        string requestUri = BuildRequestUri(NormalizeEndpoint(endpoint), queryString);

        using HttpRequestMessage message = new(HttpMethod.Post, requestUri)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };

        using HttpResponseMessage response = await HttpClient
            .SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        await using Stream contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        UiListDataResponse<BuildingInfo>? payload = await JsonSerializer
            .DeserializeAsync<UiListDataResponse<BuildingInfo>>(contentStream, _serializerOptions, cancellationToken)
            .ConfigureAwait(false);

        return payload ?? new UiListDataResponse<BuildingInfo>();
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

    private static string BuildUiListDataQuery(BuildingUiListDataRequest request)
    {
        List<string> parts = [];

        if (request.NavigationId is int navigationId)
        {
            parts.Add(FormQueryParameter("navigationId", navigationId.ToString(CultureInfo.InvariantCulture)));
        }

        parts.Add(FormQueryParameter("includePropertyValues", request.IncludePropertyValues ? "true" : "false"));

        if (request.PropertyIds is { Count: > 0 })
        {
            foreach (int propertyId in request.PropertyIds)
            {
                parts.Add(FormQueryParameter("propertyIds[]", propertyId.ToString(CultureInfo.InvariantCulture)));
            }
        }

        if (request.BuildingIds is { Count: > 0 })
        {
            foreach (int buildingId in request.BuildingIds)
            {
                parts.Add(FormQueryParameter("buildingIds[]", buildingId.ToString(CultureInfo.InvariantCulture)));
            }
        }

        return string.Join('&', parts);
    }

    private static string FormQueryParameter(string name, string value)
        => $"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value)}";

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

    private async Task<IReadOnlyDictionary<int, TValue>> QueryDictionaryAsync<TValue>(string requestPath, string queryString, CancellationToken cancellationToken)
where TValue : class
    {
        string requestUri = BuildRequestUri(requestPath, queryString);

        using Stream contentStream = await GetContentStreamAsync(requestUri, cancellationToken).ConfigureAwait(false);
        Dictionary<int, TValue>? payload = await JsonSerializer.DeserializeAsync<Dictionary<int, TValue>>(contentStream, _serializerOptions, cancellationToken).ConfigureAwait(false);

        return payload ?? [];
    }

    private async Task<Stream> GetContentStreamAsync(string requestUri, CancellationToken cancellationToken)
    {
        HttpResponseMessage response = await HttpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        }

        string? errorBody = null;
        try
        {
            errorBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception readException)
        {
            throw new PythagorasApiException(
                $"Pythagoras API request failed with status code {response.StatusCode}.",
                response.StatusCode,
                errorBody,
                readException);
        }

        throw new PythagorasApiException(
            $"Pythagoras API request failed with status code {response.StatusCode}.",
            response.StatusCode,
            errorBody);
    }

    private Task<IReadOnlyDictionary<int, CalculatedPropertyValueDto>> GetCalculatedPropertyValuesAsync(string entityType, long entityId, CalculatedPropertyValueRequest? request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(entityType))
        {
            throw new ArgumentException("Entity type must be non-empty.", nameof(entityType));
        }

        if (entityId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(entityId), "Entity id must be positive.");
        }

        string endpoint = $"rest/v1/{entityType}/{entityId}/property/calculatedvalue";
        string requestPath = NormalizeEndpoint(endpoint);
        string queryString = request?.BuildQueryString() ?? string.Empty;

        return QueryDictionaryAsync<CalculatedPropertyValueDto>(requestPath, queryString, cancellationToken);
    }
}
