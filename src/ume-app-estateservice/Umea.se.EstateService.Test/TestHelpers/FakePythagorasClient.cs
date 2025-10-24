using System.Collections.Concurrent;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enum;

namespace Umea.se.EstateService.Test.TestHelpers;

/// <summary>
/// Minimal fake of <see cref="IPythagorasClient"/> that captures requests and returns configured results.
/// </summary>
public sealed class FakePythagorasClient : IPythagorasClient
{
    private readonly ConcurrentDictionary<Type, Queue<object>> _results = new();

    /// <summary>
    /// Captured requests in invocation order.
    /// </summary>
    public List<RequestCapture> Requests { get; } = [];

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

    public Task<IReadOnlyList<T>> GetAsync<T>(string endpoint, PythagorasQuery<T>? query, CancellationToken cancellationToken) where T : class, IPythagorasDto
    {
        string? queryString = query?.BuildAsQueryString();
        Requests.Add(new RequestCapture(typeof(T), endpoint, query, queryString, cancellationToken));

        if (_results.TryGetValue(typeof(T), out Queue<object>? queue) && queue.Count > 0)
        {
            return Task.FromResult((IReadOnlyList<T>)queue.Dequeue());
        }

        return Task.FromResult<IReadOnlyList<T>>([]);
    }

    public IAsyncEnumerable<T> GetPaginatedAsync<T>(string endpoint, PythagorasQuery<T>? query, int pageSize, CancellationToken cancellationToken) where T : class, IPythagorasDto
        => throw new NotSupportedException();

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
    public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("SendAsync is not supported by FakePythagorasClient. Use a dedicated fake for blueprint tests.");
    }

    public Func<int, BlueprintFormat, bool, CancellationToken, Task<HttpResponseMessage>>? OnGetFloorBlueprintAsync { get; set; }

    public Task<HttpResponseMessage> GetFloorBlueprintAsync(int floorId, BlueprintFormat format, bool includeWorkspaceTexts, CancellationToken cancellationToken = default)
    {
        if (OnGetFloorBlueprintAsync is null)
        {
            throw new NotSupportedException("Configure OnGetFloorBlueprintAsync before calling this method.");
        }

        return OnGetFloorBlueprintAsync(floorId, format, includeWorkspaceTexts, cancellationToken);
    }

    public Task<IReadOnlyDictionary<int, TValue>> GetDictionaryAsync<TValue>(string endpoint, PythagorasQuery<TValue>? query, CancellationToken cancellationToken = default) where TValue : class
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyDictionary<int, CalculatedPropertyValueDto>> GetBuildingCalculatedPropertyValuesAsync(int buildingId, CalculatedPropertyValueRequest? request = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
