using System.Collections.Concurrent;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api.Request;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enum;

namespace Umea.se.EstateService.Test.TestHelpers;

/// <summary>
/// Minimal fake of <see cref="IPythagorasClient"/> that captures requests and returns configured results.
/// </summary>
public sealed class FakePythagorasClient : IPythagorasClient
{
    private readonly ConcurrentDictionary<Type, Queue<object>> _results = new();
    private readonly Queue<IReadOnlyDictionary<int, CalculatedPropertyValueDto>> _calculatedPropertyResults = new();
    private readonly Queue<UiListDataResponse<BuildingInfo>> _buildingUiListDataResults = new();
    private readonly Queue<UiListDataResponse<NavigationFolder>> _navigationFolderUiListDataResults = new();

    /// <summary>
    /// Captured requests in invocation order.
    /// </summary>
    public List<RequestCapture> Requests { get; } = [];

    public List<CalculatedPropertyRequestCapture> CalculatedPropertyRequests { get; } = [];
    public List<BuildingUiListDataRequestCapture> BuildingUiListDataRequests { get; } = [];
    public List<NavigationFolderUiListDataRequestCapture> NavigationFolderUiListDataRequests { get; } = [];

    public string? LastEndpoint => Requests.LastOrDefault().Endpoint;

    public string? LastQueryString => Requests.LastOrDefault().QueryString;

    public object? LastQuery => Requests.LastOrDefault().Query;

    public CancellationToken LastCancellationToken => Requests.LastOrDefault().CancellationToken;

    public bool GetAsyncCalled => Requests.Count > 0;

    public int GetAsyncCallCount => Requests.Count;

    public IEnumerable<string> EndpointsCalled => Requests.Select(r => r.Endpoint);

    /// <summary>
    /// Replaces any previously configured results for type <typeparamref name="T"/> with a single result.
    /// </summary>
    public void SetGetAsyncResult<T>(IReadOnlyList<T> result) where T : class, IPythagorasDto
    {
        Queue<object> queue = GetQueue(typeof(T));
        queue.Clear();
        queue.Enqueue(result);
    }

    /// <summary>
    /// Convenience overload accepting params.
    /// </summary>
    public void SetGetAsyncResult<T>(params T[] items) where T : class, IPythagorasDto
    {
        SetGetAsyncResult((IReadOnlyList<T>)items);
    }

    /// <summary>
    /// Enqueues a result that will be returned on the next call for <typeparamref name="T"/>.
    /// </summary>
    public void EnqueueGetAsyncResult<T>(IReadOnlyList<T> result) where T : class, IPythagorasDto
    {
        GetQueue(typeof(T)).Enqueue(result);
    }

    /// <summary>
    /// Convenience overload accepting params.
    /// </summary>
    public void EnqueueGetAsyncResult<T>(params T[] items) where T : class, IPythagorasDto
    {
        EnqueueGetAsyncResult((IReadOnlyList<T>)items);
    }

    /// <summary>
    /// Returns all captured requests matching <typeparamref name="T"/>.
    /// </summary>
    public IEnumerable<RequestCapture> GetRequestsFor<T>() where T : class, IPythagorasDto
        => Requests.Where(r => r.DtoType == typeof(T));

    public Task<IReadOnlyList<BuildingInfo>> GetBuildingsAsync(PythagorasQuery<BuildingInfo>? query = null, CancellationToken cancellationToken = default)
        => CaptureAsync("rest/v1/building/info", query, cancellationToken);

    public Task<IReadOnlyList<BuildingAscendant>> GetBuildingAscendantsAsync(int buildingId, PythagorasQuery<BuildingAscendant>? query = null, CancellationToken cancellationToken = default)
    {
        if (buildingId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(buildingId), "Building id must be positive.");
        }

        return CaptureAsync($"rest/v1/building/{buildingId}/node/ascendant", query, cancellationToken);
    }

    public Task<IReadOnlyList<Floor>> GetBuildingFloorsAsync(int buildingId, PythagorasQuery<Floor>? query = null, CancellationToken cancellationToken = default)
    {
        if (buildingId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(buildingId), "Building id must be positive.");
        }

        return CaptureAsync($"rest/v1/building/{buildingId}/floor", query, cancellationToken);
    }

    public Task<IReadOnlyList<Workspace>> GetWorkspacesAsync(PythagorasQuery<Workspace>? query = null, CancellationToken cancellationToken = default)
        => CaptureAsync("rest/v1/workspace/info", query, cancellationToken);

    public Task<IReadOnlyList<NavigationFolder>> GetNavigationFoldersAsync(PythagorasQuery<NavigationFolder>? query = null, CancellationToken cancellationToken = default)
        => CaptureAsync("rest/v1/navigationfolder/info", query, cancellationToken);

    public Task<IReadOnlyList<Floor>> GetFloorsAsync(PythagorasQuery<Floor>? query = null, CancellationToken cancellationToken = default)
        => CaptureAsync("rest/v1/floor/info", query, cancellationToken);

    public Task<IReadOnlyList<GalleryImageFile>> GetBuildingGalleryImagesAsync(int buildingId, CancellationToken cancellationToken = default)
    {
        if (buildingId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(buildingId), "Building id must be positive.");
        }

        return CaptureAsync<GalleryImageFile>($"rest/v1/building/{buildingId}/galleryimagefile", query: null, cancellationToken);
    }

    public Func<int, GalleryImageVariant, CancellationToken, Task<HttpResponseMessage>>? OnGetGalleryImageDataAsync { get; set; }

    public Task<HttpResponseMessage> GetGalleryImageDataAsync(int imageId, GalleryImageVariant variant, CancellationToken cancellationToken = default)
    {
        if (imageId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(imageId), "Image id must be positive.");
        }

        if (OnGetGalleryImageDataAsync is null)
        {
            throw new NotSupportedException("Configure OnGetGalleryImageDataAsync before calling this method.");
        }

        return OnGetGalleryImageDataAsync(imageId, variant, cancellationToken);
    }

    private Task<IReadOnlyList<T>> CaptureAsync<T>(string endpoint, PythagorasQuery<T>? query, CancellationToken cancellationToken) where T : class, IPythagorasDto
    {
        PythagorasQuery<T>? originalQuery = query;
        PythagorasQuery<T> effectiveQuery = originalQuery ?? new PythagorasQuery<T>();
        string queryString = effectiveQuery.BuildAsQueryString();
        Requests.Add(new RequestCapture(typeof(T), endpoint, originalQuery, queryString, cancellationToken));

        if (_results.TryGetValue(typeof(T), out Queue<object>? queue) && queue.Count > 0)
        {
            return Task.FromResult((IReadOnlyList<T>)queue.Dequeue());
        }

        return Task.FromResult<IReadOnlyList<T>>([]);
    }

    private Queue<object> GetQueue(Type dtoType)
    {
        return _results.GetOrAdd(dtoType, _ => new Queue<object>());
    }

    /// <summary>
    /// Clears configured results and captured requests.
    /// </summary>
    public void Reset()
    {
        _results.Clear();
        Requests.Clear();
        _calculatedPropertyResults.Clear();
        CalculatedPropertyRequests.Clear();
        BuildingUiListDataRequests.Clear();
        _buildingUiListDataResults.Clear();
        NavigationFolderUiListDataRequests.Clear();
        _navigationFolderUiListDataResults.Clear();
        OnGetGalleryImageDataAsync = null;
    }

    /// <summary>
    /// Captures metadata about a single request.
    /// </summary>
    public readonly record struct RequestCapture(
        Type DtoType,
        string Endpoint,
        object? Query,
        string? QueryString,
        CancellationToken CancellationToken);

    public readonly record struct CalculatedPropertyRequestCapture(
        string Endpoint,
        int EntityId,
        CalculatedPropertyValueRequest? Request,
        CancellationToken CancellationToken);

    public readonly record struct BuildingUiListDataRequestCapture(
        BuildingUiListDataRequest Request,
        CancellationToken CancellationToken);

    public readonly record struct NavigationFolderUiListDataRequestCapture(
        NavigationFolderUiListDataRequest Request,
        CancellationToken CancellationToken);

    public void SetCalculatedPropertyValuesResult(IReadOnlyDictionary<int, CalculatedPropertyValueDto> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        _calculatedPropertyResults.Clear();
        _calculatedPropertyResults.Enqueue(result);
    }

    public void SetBuildingUiListDataResponse(UiListDataResponse<BuildingInfo> response)
    {
        ArgumentNullException.ThrowIfNull(response);
        _buildingUiListDataResults.Clear();
        _buildingUiListDataResults.Enqueue(response);
    }

    public void EnqueueBuildingUiListDataResponse(UiListDataResponse<BuildingInfo> response)
    {
        ArgumentNullException.ThrowIfNull(response);
        _buildingUiListDataResults.Enqueue(response);
    }

    public void SetNavigationFolderUiListDataResponse(UiListDataResponse<NavigationFolder> response)
    {
        ArgumentNullException.ThrowIfNull(response);
        _navigationFolderUiListDataResults.Clear();
        _navigationFolderUiListDataResults.Enqueue(response);
    }

    public void EnqueueNavigationFolderUiListDataResponse(UiListDataResponse<NavigationFolder> response)
    {
        ArgumentNullException.ThrowIfNull(response);
        _navigationFolderUiListDataResults.Enqueue(response);
    }

    public void EnqueueCalculatedPropertyValuesResult(IReadOnlyDictionary<int, CalculatedPropertyValueDto> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        _calculatedPropertyResults.Enqueue(result);
    }

    public Func<int, BlueprintFormat, IDictionary<int, IReadOnlyList<string>>?, CancellationToken, Task<HttpResponseMessage>>? OnGetFloorBlueprintAsync { get; set; }

    public Task<HttpResponseMessage> GetFloorBlueprintAsync(
        int floorId,
        BlueprintFormat format,
        IDictionary<int, IReadOnlyList<string>>? workspaceTexts,
        CancellationToken cancellationToken = default)
    {
        if (OnGetFloorBlueprintAsync is null)
        {
            throw new NotSupportedException("Configure OnGetFloorBlueprintAsync before calling this method.");
        }

        return OnGetFloorBlueprintAsync(floorId, format, workspaceTexts, cancellationToken);
    }

    public Task<IReadOnlyDictionary<int, CalculatedPropertyValueDto>> GetBuildingCalculatedPropertyValuesAsync(int buildingId, CalculatedPropertyValueRequest? request = null, CancellationToken cancellationToken = default)
    {
        if (buildingId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(buildingId), "Building id must be positive.");
        }

        return CaptureCalculatedPropertyValuesAsync($"rest/v1/building/{buildingId}/property/calculatedvalue", buildingId, request, cancellationToken);
    }

    public Task<IReadOnlyDictionary<int, CalculatedPropertyValueDto>> GetCalculatedPropertyValuesForEstateAsync(int estateId, CalculatedPropertyValueRequest? request = null, CancellationToken cancellationToken = default)
    {
        if (estateId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(estateId), "Estate id must be positive.");
        }

        return CaptureCalculatedPropertyValuesAsync($"rest/v1/navigationfolder/{estateId}/property/calculatedvalue", estateId, request, cancellationToken);
    }

    public Task<UiListDataResponse<BuildingInfo>> PostBuildingUiListDataAsync(BuildingUiListDataRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        BuildingUiListDataRequests.Add(new BuildingUiListDataRequestCapture(request, cancellationToken));

        if (_buildingUiListDataResults.Count > 0)
        {
            return Task.FromResult(_buildingUiListDataResults.Dequeue());
        }

        return Task.FromResult(new UiListDataResponse<BuildingInfo>());
    }

    public Task<UiListDataResponse<NavigationFolder>> PostNavigationFolderUiListDataAsync(NavigationFolderUiListDataRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        NavigationFolderUiListDataRequests.Add(new NavigationFolderUiListDataRequestCapture(request, cancellationToken));

        if (_navigationFolderUiListDataResults.Count > 0)
        {
            return Task.FromResult(_navigationFolderUiListDataResults.Dequeue());
        }

        return Task.FromResult(new UiListDataResponse<NavigationFolder>());
    }

    private Task<IReadOnlyDictionary<int, CalculatedPropertyValueDto>> CaptureCalculatedPropertyValuesAsync(string endpoint, int entityId, CalculatedPropertyValueRequest? request, CancellationToken cancellationToken)
    {
        CalculatedPropertyRequests.Add(new CalculatedPropertyRequestCapture(endpoint, entityId, request, cancellationToken));

        // Also add to general Requests list so it appears in EndpointsCalled
        Requests.Add(new RequestCapture(typeof(CalculatedPropertyValueDto), endpoint, request, null, cancellationToken));

        if (_calculatedPropertyResults.Count > 0)
        {
            return Task.FromResult(_calculatedPropertyResults.Dequeue());
        }

        return Task.FromResult<IReadOnlyDictionary<int, CalculatedPropertyValueDto>>(new Dictionary<int, CalculatedPropertyValueDto>());
    }
}
