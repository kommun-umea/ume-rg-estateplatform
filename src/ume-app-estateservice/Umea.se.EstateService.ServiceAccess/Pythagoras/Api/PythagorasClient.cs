using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enums;
using Umea.se.Toolkit.ExternalService;

namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Api;

public sealed class PythagorasClient(IHttpClientFactory httpClientFactory) : ExternalServiceBase(HttpClientNames.Pythagoras, httpClientFactory), IPythagorasClient
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    private const string PythagorasApplicationName = "se.pythagoras.pythagorasweb";

    private static readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
        Converters =
        {
            new UnixMillisDateTimeConverter()
        }
    };

    private static readonly JsonSerializerOptions _workOrderSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    protected override string PingUrl => "";

    public Task<IReadOnlyList<BuildingInfo>> GetBuildingsAsync(PythagorasQuery<BuildingInfo>? query = null, CancellationToken cancellationToken = default)
    {
        query ??= new PythagorasQuery<BuildingInfo>();
        return QueryAsync("rest/v1/building/info", query, cancellationToken);
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

    public Task<IReadOnlyList<FileDocumentDirectory>> GetBuildingRootDirectories(int buildingId, PythagorasQuery<FileDocumentDirectory>? query = null, CancellationToken cancellationToken = default)
    {
        query ??= new PythagorasQuery<FileDocumentDirectory>();
        string endpoint = $"rest/v1/building/{buildingId}/documentfolder/info/root";
        return QueryAsync(endpoint, query, cancellationToken);
    }

    public Task<IReadOnlyList<FileDocument>> GetBuildingRootDocuments(int buildingId, PythagorasQuery<FileDocument>? query = null, CancellationToken cancellationToken = default)
    {
        query ??= new PythagorasQuery<FileDocument>();
        string endpoint = $"rest/v1/building/{buildingId}/documentfile/root";
        return QueryAsync(endpoint, query, cancellationToken);
    }

    public Task<FileDocumentDirectory?> GetDirectory(int directoryId, CancellationToken cancellationToken = default)
    {
        string endpoint = $"rest/v1/documentfolder/{directoryId}/info";
        return GetAsync<FileDocumentDirectory>(endpoint, cancellationToken);
    }

    public Task<IReadOnlyList<FileDocumentDirectory>> GetChildDirectories(int directoryId, PythagorasQuery<FileDocumentDirectory>? query = null, CancellationToken cancellationToken = default)
    {
        query ??= new PythagorasQuery<FileDocumentDirectory>();
        string endpoint = $"rest/v1/documentfolder/{directoryId}/info/child";
        return QueryAsync(endpoint, query, cancellationToken);
    }

    public Task<IReadOnlyList<FileDocument>> GetDirectoryDocuments(int directoryId, PythagorasQuery<FileDocument>? query = null, CancellationToken cancellationToken = default)
    {
        query ??= new PythagorasQuery<FileDocument>();
        string endpoint = $"rest/v1/documentfolder/{directoryId}/documentfile";
        return QueryAsync(endpoint, query, cancellationToken);
    }

    public Task<(byte[] data, string contentType)> GetDocument(int documentId, CancellationToken cancellationToken = default)
    {
        string endpoint = $"rest/v1/documentfile/{documentId}/data";
        return GetFileAsync(endpoint, cancellationToken);
    }

    public async Task<UiListDataResponse<FileDocument>> GetBuildingDocumentListAsync(int buildingId, int? maxResults = null, CancellationToken cancellationToken = default)
    {
        string endpoint = $"rest/v1/building/{buildingId}/documentfile/uilistdata";
        string queryString = maxResults.HasValue ? $"maxResults={maxResults.Value}" : string.Empty;
        string requestUri = BuildRequestUri(NormalizeEndpoint(endpoint), queryString);

        using HttpResponseMessage response = await HttpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
        UiListDataResponse<FileDocument>? result = await ProcessResponseAsync<UiListDataResponse<FileDocument>>(response, cancellationToken).ConfigureAwait(false);
        return result ?? new UiListDataResponse<FileDocument>();
    }

    public async Task<UiListDataResponse<FileDocument>> GetDocumentListAsync(int? maxResults = null, string? orderBy = null, bool orderAsc = true, CancellationToken cancellationToken = default)
    {
        List<string> parts = [];
        if (maxResults.HasValue) parts.Add($"maxResults={maxResults.Value}");
        if (orderBy is not null) parts.Add($"orderBy={orderBy}");
        if (!orderAsc) parts.Add("orderAsc=false");

        string requestUri = BuildRequestUri(
            NormalizeEndpoint("rest/v1/documentfile/uilistdata"),
            string.Join("&", parts));

        using HttpResponseMessage response = await HttpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
        UiListDataResponse<FileDocument>? result = await ProcessResponseAsync<UiListDataResponse<FileDocument>>(response, cancellationToken).ConfigureAwait(false);
        return result ?? new UiListDataResponse<FileDocument>();
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

    public Task<IReadOnlyList<BusinessType>> GetBusinessTypesAsync(PythagorasQuery<BusinessType>? query = null, CancellationToken cancellationToken = default)
    {
        query ??= new PythagorasQuery<BusinessType>();
        return QueryAsync("rest/v1/businesstype", query, cancellationToken);
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

    public async Task<UiListDataResponse<BuildingInfo>> PostBuildingUiListDataAsync(BuildingUiListDataRequest request, CancellationToken cancellationToken = default)
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
        string queryString = BuildUiListDataQuery(request.NavigationId, request.IncludePropertyValues, request.IncludeNavigationInfo, request.PropertyIds, request.BuildingIds, "buildingIds[]");
        return await PostAsync<UiListDataResponse<BuildingInfo>>(endpoint, queryString, cancellationToken).ConfigureAwait(false);
    }

    public async Task<UiListDataResponse<NavigationFolder>> PostNavigationFolderUiListDataAsync(
        NavigationFolderUiListDataRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.NavigationFolderIds is { Count: > 0 })
        {
            foreach (int navigationFolderId in request.NavigationFolderIds)
            {
                if (navigationFolderId <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(request), "Navigation folder ids must be positive.");
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

        string endpoint = "rest/v1/navigationfolder/info/uilistdata";
        string queryString = BuildNavigationFolderUiListDataQuery(
            request.NavigationId,
            request.IncludePropertyValues,
            request.IncludeAscendantBuildings,
            request.PropertyIds,
            request.NavigationFolderIds);
        return await PostAsync<UiListDataResponse<NavigationFolder>>(endpoint, queryString, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<TDto>> QueryAsync<TDto>(string endpoint, PythagorasQuery<TDto> query, CancellationToken cancellationToken)
        where TDto : class, IPythagorasDto
    {
        ArgumentNullException.ThrowIfNull(query);
        return await GetListAsync<TDto>(endpoint, query.BuildAsQueryString(), cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<TDto>> GetListAsync<TDto>(string endpoint, string queryString, CancellationToken cancellationToken)
    {
        string requestUri = BuildRequestUri(NormalizeEndpoint(endpoint), queryString);

        using HttpResponseMessage response = await HttpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
        List<TDto>? payload = await ProcessResponseAsync<List<TDto>>(response, cancellationToken).ConfigureAwait(false);

        return payload ?? [];
    }

    private async Task<TDto?> GetAsync<TDto>(string endpoint, CancellationToken cancellationToken)
    {
        string requestUri = NormalizeEndpoint(endpoint);

        using HttpResponseMessage response = await HttpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }

        TDto? payload = await ProcessResponseAsync<TDto>(response, cancellationToken).ConfigureAwait(false);

        return payload;
    }

    private async Task<(byte[] data, string contentType)> GetFileAsync(string endpoint, CancellationToken cancellationToken)
    {
        string requestUri = NormalizeEndpoint(endpoint);

        using HttpResponseMessage response = await HttpClient.GetAsync(requestUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        string contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
        byte[] data = await response.Content.ReadAsByteArrayAsync(cancellationToken);

        return (data, contentType);
    }

    private async Task<TResponse> PostAsync<TResponse>(string endpoint, string queryString, CancellationToken cancellationToken)
        where TResponse : class, new()
    {
        string requestUri = BuildRequestUri(NormalizeEndpoint(endpoint), queryString);

        using HttpRequestMessage message = new(HttpMethod.Post, requestUri)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };

        using HttpResponseMessage response = await HttpClient
            .SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        TResponse? payload = await ProcessResponseAsync<TResponse>(response, cancellationToken).ConfigureAwait(false);
        return payload ?? new TResponse();
    }

    private static async Task<T?> ProcessResponseAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        response.EnsureSuccessStatusCode();

        using Stream contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        return await JsonSerializer.DeserializeAsync<T>(contentStream, _serializerOptions, cancellationToken).ConfigureAwait(false);
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

    private static string BuildUiListDataQuery(
        int? navigationId,
        bool includePropertyValues,
        bool includeNavigationInfo,
        IReadOnlyCollection<int>? propertyIds,
        IReadOnlyCollection<int>? entityIds,
        string entityIdParameterName)
    {
        List<string> parts = [];

        if (navigationId is int value)
        {
            parts.Add(FormQueryParameter("navigationId", value.ToString(CultureInfo.InvariantCulture)));
        }

        parts.Add(FormQueryParameter("includePropertyValues", includePropertyValues ? "true" : "false"));

        if (includeNavigationInfo)
        {
            parts.Add(FormQueryParameter("includeNavigationInfo", "true"));
        }

        if (propertyIds is { Count: > 0 })
        {
            foreach (int propertyId in propertyIds)
            {
                parts.Add(FormQueryParameter("propertyIds[]", propertyId.ToString(CultureInfo.InvariantCulture)));
            }
        }

        if (entityIds is { Count: > 0 })
        {
            foreach (int entityId in entityIds)
            {
                parts.Add(FormQueryParameter(entityIdParameterName, entityId.ToString(CultureInfo.InvariantCulture)));
            }
        }

        return string.Join('&', parts);
    }

    private static string BuildNavigationFolderUiListDataQuery(
        int? navigationId,
        bool includePropertyValues,
        bool includeAscendantBuildings,
        IReadOnlyCollection<int>? propertyIds,
        IReadOnlyCollection<int>? navigationFolderIds)
    {
        List<string> parts = [];

        if (navigationId is int value)
        {
            parts.Add(FormQueryParameter("navigationId", value.ToString(CultureInfo.InvariantCulture)));
        }

        parts.Add(FormQueryParameter("includePropertyValues", includePropertyValues ? "true" : "false"));
        parts.Add(FormQueryParameter("includeAscendantBuildings", includeAscendantBuildings ? "true" : "false"));

        if (propertyIds is { Count: > 0 })
        {
            foreach (int propertyId in propertyIds)
            {
                parts.Add(FormQueryParameter("propertyIds[]", propertyId.ToString(CultureInfo.InvariantCulture)));
            }
        }

        if (navigationFolderIds is { Count: > 0 })
        {
            foreach (int folderId in navigationFolderIds)
            {
                parts.Add(FormQueryParameter("navigationFolderIds[]", folderId.ToString(CultureInfo.InvariantCulture)));
            }
        }

        return string.Join('&', parts);
    }

    private static string FormQueryParameter(string name, string value) => $"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value)}";
    public Task<HttpResponseMessage> GetFloorBlueprintAsync(int floorId, BlueprintFormat format, IDictionary<int, IReadOnlyList<string>>? workspaceTexts, CancellationToken cancellationToken = default)
    {
        if (floorId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(floorId), "Floor id must be positive.");
        }

        string endpoint = $"rest/v1/floor/{floorId}/gmodel/print/{FormatToSegment(format)}";
        FloorBlueprintRequestPayload payload = FloorBlueprintRequestPayload.CreateDefault();

        if (workspaceTexts is not null)
        {
            payload = payload.WithWorkspaceTexts(workspaceTexts);
        }

        HttpRequestMessage request = new(HttpMethod.Post, endpoint)
        {
            Content = BuildBlueprintContent(payload)
        };

        // Blueprint rendering is significantly slower than other Pythagoras endpoints.
        // Uses a dedicated HttpClient with higher timeouts matching the SVG FusionCache budget.
        HttpClient blueprintClient = _httpClientFactory.CreateClient(HttpClientNames.PythagorasBlueprints);
        return blueprintClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
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
            throw new PythagorasApiException($"Pythagoras API request failed with status code {response.StatusCode}.", response.StatusCode, errorBody, readException);
        }

        throw new PythagorasApiException($"Pythagoras API request failed with status code {response.StatusCode}.", response.StatusCode, errorBody);
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

    public Task<IReadOnlyList<GalleryImageFile>> GetBuildingGalleryImagesAsync(int buildingId, CancellationToken cancellationToken = default)
    {
        if (buildingId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(buildingId), "Building id must be positive.");
        }

        string endpoint = $"rest/v1/building/{buildingId}/galleryimagefile";
        PythagorasQuery<GalleryImageFile> query = new();

        return QueryAsync(endpoint, query, cancellationToken);
    }

    public Task<HttpResponseMessage> GetGalleryImageDataAsync(int imageId, GalleryImageVariant variant, CancellationToken cancellationToken = default)
    {
        if (imageId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(imageId), "Image id must be positive.");
        }

        string segment = variant switch
        {
            GalleryImageVariant.Thumbnail => "thumbnail/data",
            GalleryImageVariant.Original => "data",
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, "Unsupported gallery image variant.")
        };

        string endpoint = $"rest/v1/galleryimagefile/{imageId}/{segment}";
        string queryString = FormQueryParameter("pyApp", PythagorasApplicationName);
        string requestUri = BuildRequestUri(NormalizeEndpoint(endpoint), queryString);

        // Gallery image fetches use a dedicated HttpClient with tighter timeouts so they fit
        // inside the raster FusionCache FactoryHardTimeout. See Program.cs for configuration.
        // Default content buffering (not ResponseHeadersRead) so the body read is covered by
        // Polly's timeout budget, not FusionCache's hard timeout. The caller already loads the
        // full image into memory, so buffering here doesn't change the memory model.
        HttpClient imageClient = _httpClientFactory.CreateClient(HttpClientNames.PythagorasImages);
        HttpRequestMessage request = new(HttpMethod.Get, requestUri);
        return imageClient.SendAsync(request, cancellationToken);
    }

    public async Task<WorkOrderDto?> CreateWorkOrderAsync(PythagorasWorkOrderType workOrderType, PythagorasWorkOrderOrigin origin, CreatePythagorasWorkOrderRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        int typeId = (int)workOrderType;
        string endpoint = $"rest/v1/workordertype/{typeId}/workorder/newdefault";
        string queryString = FormQueryParameter("origin", origin.ToString());
        string requestUri = BuildRequestUri(NormalizeEndpoint(endpoint), queryString);

        string json = JsonSerializer.Serialize(request, _workOrderSerializerOptions);

        using HttpRequestMessage message = new(HttpMethod.Post, requestUri)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        using HttpResponseMessage response = await HttpClient
            .SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new PythagorasApiException(
                $"CreateWorkOrder failed: {(int)response.StatusCode} {response.ReasonPhrase}. Request: {json}. Response: {responseBody}",
                response.StatusCode,
                responseBody);
        }

        try
        {
            return JsonSerializer.Deserialize<WorkOrderDto>(responseBody, _serializerOptions);
        }
        catch (JsonException ex)
        {
            // Even if full deserialization fails, try to extract the ID so retries don't create duplicates
            int? id = TryExtractWorkOrderId(responseBody);
            if (id.HasValue)
            {
                return new WorkOrderDto { Id = id.Value };
            }

            throw new PythagorasApiException(
                $"CreateWorkOrder succeeded but response deserialization failed. Response: {responseBody}",
                response.StatusCode,
                responseBody,
                ex);
        }
    }

    private static int? TryExtractWorkOrderId(string json)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("id", out JsonElement idElement) && idElement.TryGetInt32(out int id))
            {
                return id;
            }
        }
        catch
        {
            // Ignore parse failures
        }

        return null;
    }

    public async Task UploadWorkOrderDocumentAsync(int workOrderId, Stream fileStream, string fileName, long fileSize, int? parentId = null, int? actionTypeId = null, int? actionTypeStatusId = null, CancellationToken cancellationToken = default)
    {
        if (workOrderId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(workOrderId), "Work order id must be positive.");
        }

        string endpoint = $"rest/v1/workorder/{workOrderId}/documentfile/record";
        string requestUri = NormalizeEndpoint(endpoint);

        using MultipartFormDataContent content = new();
        StreamContent fileContent = new(fileStream);
        content.Add(fileContent, "file", fileName);
        content.Add(new StringContent(fileName), "fileName");
        content.Add(new StringContent(fileSize.ToString(CultureInfo.InvariantCulture)), "fileSize");
        content.Add(new StringContent((parentId ?? 0).ToString(CultureInfo.InvariantCulture)), "parentId");

        if (actionTypeId.HasValue)
        {
            var fileRecordData = new
            {
                actionTypeId = actionTypeId.Value,
                actionTypeStatusId = actionTypeStatusId,
                receivedDate = (string?)null,
                isRecorded = false,
                isGDPR = false,
                removalProcess = (string?)null
            };
            string fileRecordDataJson = JsonSerializer.Serialize(fileRecordData, _workOrderSerializerOptions);
            content.Add(new StringContent(fileRecordDataJson), "fileRecordData");
        }

        using HttpResponseMessage response = await HttpClient
            .PostAsync(requestUri, content, cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
    }

    public Task<WorkOrderDto?> GetWorkOrderAsync(int workOrderId, CancellationToken cancellationToken = default)
    {
        if (workOrderId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(workOrderId), "Work order id must be positive.");
        }

        string endpoint = $"rest/v1/workorder/{workOrderId}/info";
        return GetAsync<WorkOrderDto>(endpoint, cancellationToken);
    }

    public Task<IReadOnlyList<WorkOrderDto>> GetWorkOrdersByIdsAsync(IReadOnlyList<int> workOrderIds, CancellationToken cancellationToken = default)
    {
        if (workOrderIds is not { Count: > 0 })
        {
            return Task.FromResult<IReadOnlyList<WorkOrderDto>>([]);
        }

        PythagorasQuery<WorkOrderDto> query = new PythagorasQuery<WorkOrderDto>()
            .WithIds([.. workOrderIds]);

        return QueryAsync("rest/v1/workorder", query, cancellationToken);
    }

    public async Task<IReadOnlyList<WorkOrderInfoDto>> GetWorkOrderInfosByIdsAsync(IReadOnlyList<int> workOrderIds, CancellationToken cancellationToken = default)
    {
        if (workOrderIds is not { Count: > 0 })
        {
            return [];
        }

        string requestUri = NormalizeEndpoint("rest/v1/workorder/info");
        string json = JsonSerializer.Serialize(workOrderIds, _serializerOptions);

        using HttpRequestMessage message = new(HttpMethod.Post, requestUri)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        using HttpResponseMessage response = await HttpClient
            .SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            string? errorBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new PythagorasApiException(
                $"GetWorkOrderInfosByIds failed: {(int)response.StatusCode} {response.ReasonPhrase}.",
                response.StatusCode,
                errorBody);
        }

        await using Stream contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        List<WorkOrderInfoDto>? result = await JsonSerializer.DeserializeAsync<List<WorkOrderInfoDto>>(contentStream, _serializerOptions, cancellationToken).ConfigureAwait(false);

        return result ?? [];
    }

    public Task<IReadOnlyList<WorkOrderCategoryInfoDto>> GetWorkOrderCategoriesAsync(int moduleId, CancellationToken cancellationToken = default)
    {
        if (moduleId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(moduleId), "Module id must be positive.");
        }

        string endpoint = $"rest/v1/workordermodule/{moduleId}/workordercategory/info";
        PythagorasQuery<WorkOrderCategoryInfoDto> query = new();
        return QueryAsync(endpoint, query, cancellationToken);
    }

    public async Task<WorkOrderDto?> SetWorkOrderCategoryAsync(int workOrderId, int categoryId, CancellationToken cancellationToken = default)
    {
        if (workOrderId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(workOrderId), "Work order id must be positive.");
        }

        if (categoryId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(categoryId), "Category id must be positive.");
        }

        string endpoint = $"rest/v1/workorder/{workOrderId}/category";
        string requestUri = NormalizeEndpoint(endpoint);
        string json = JsonSerializer.Serialize(categoryId);

        using HttpRequestMessage message = new(HttpMethod.Put, requestUri)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        using HttpResponseMessage response = await HttpClient
            .SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        return await ProcessResponseAsync<WorkOrderDto>(response, cancellationToken).ConfigureAwait(false);
    }

    public Task<IReadOnlyList<DocumentFileRecordActionType>> GetDocumentRecordActionTypesAsync(PythagorasQuery<DocumentFileRecordActionType>? query = null, CancellationToken ct = default)
    {
        query ??= new PythagorasQuery<DocumentFileRecordActionType>();
        return QueryAsync("rest/v1/documentfilerecordactiontype", query, ct);
    }

    public Task<IReadOnlyList<DocumentFileRecordActionTypeStatus>> GetDocumentRecordActionTypeStatusesAsync(int actionTypeId, CancellationToken ct = default)
    {
        if (actionTypeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(actionTypeId), "Action type id must be positive.");
        }

        string endpoint = $"rest/v1/documentfilerecordactiontype/{actionTypeId}/status";
        PythagorasQuery<DocumentFileRecordActionTypeStatus> query = new();
        return QueryAsync(endpoint, query, ct);
    }

    public Task<IReadOnlyList<FileDocumentDirectory>> GetWorkOrderDocumentFoldersAsync(int workOrderId, PythagorasQuery<FileDocumentDirectory>? query = null, CancellationToken ct = default)
    {
        if (workOrderId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(workOrderId), "Work order id must be positive.");
        }

        query ??= new PythagorasQuery<FileDocumentDirectory>();
        return QueryAsync($"rest/v1/workorder/{workOrderId}/documentfolder/info/root", query, ct);
    }

    public Task<FileDocumentInfo?> GetDocumentInfoAsync(int documentId, CancellationToken cancellationToken = default)
    {
        if (documentId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(documentId), "Document id must be positive.");
        }

        string endpoint = $"rest/v1/documentfile/{documentId}/info";
        return GetAsync<FileDocumentInfo>(endpoint, cancellationToken);
    }
}
