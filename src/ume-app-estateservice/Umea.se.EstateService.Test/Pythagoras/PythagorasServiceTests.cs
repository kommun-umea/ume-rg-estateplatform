using Umea.se.EstateService.ServiceAccess.Pythagoras;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;

namespace Umea.se.EstateService.Test.Pythagoras;

public class PythagorasServiceTests
{
    [Fact]
    public async Task GetBuildingsAsync_DelegatesToClient()
    {
        FakePythagorasClient client = new()
        {
            GetAsyncResult = [new() { Id = 42 }]
        };
        PythagorasService service = new(client);
        using CancellationTokenSource cts = new();

        Action<PythagorasQuery<Building>> configure = query => query.WithIds(42);
        IReadOnlyList<Building> result = await service.GetBuildingsAsync(configure, cts.Token);

        Assert.True(client.GetAsyncCalled);
        Assert.Equal("rest/v1/building", client.LastEndpoint);
        Assert.Same(configure, client.LastConfigure);
        Assert.Equal(cts.Token, client.LastCancellationToken);
        Assert.Same(client.GetAsyncResult, result);
    }

    [Fact]
    public async Task GetAllBuildingsAsync_DelegatesToClient()
    {
        FakePythagorasClient client = new()
        {
            GetPaginatedAsyncResult = YieldAsync(new Building { Id = 1 }, new Building { Id = 2 })
        };
        PythagorasService service = new(client);
        using CancellationTokenSource cts = new();

        List<Building> collected = [];
        await foreach (Building building in service.GetPaginatedBuildingsAsync(null, pageSize: 10, cts.Token))
        {
            collected.Add(building);
        }

        Assert.True(client.GetPaginatedAsyncCalled);
        Assert.Equal("rest/v1/building", client.LastEndpoint);
        Assert.Null(client.LastConfigure);
        Assert.Equal(10, client.LastPageSize);
        Assert.Equal(cts.Token, client.LastCancellationToken);
        Assert.Equal([1, 2], [.. collected.Select(b => b.Id)]);
    }

    private sealed class FakePythagorasClient : IPythagorasClient
    {
        public Delegate? LastConfigure { get; private set; }
        public CancellationToken LastCancellationToken { get; private set; }
        public bool GetAsyncCalled { get; private set; }
        public bool GetPaginatedAsyncCalled { get; private set; }
        public int LastPageSize { get; private set; }
        public IReadOnlyList<Building> GetAsyncResult { get; set; } = [];
        public IAsyncEnumerable<Building> GetPaginatedAsyncResult { get; set; } = AsyncEnumerableHelper.Empty<Building>();
        public string? LastEndpoint { get; private set; }

        public Task<IReadOnlyList<TDto>> GetAsync<TDto>(string endpoint, Action<PythagorasQuery<TDto>>? configure, CancellationToken cancellationToken) where TDto : class
        {
            GetAsyncCalled = true;
            LastEndpoint = endpoint;
            LastConfigure = configure;
            LastCancellationToken = cancellationToken;

            if (typeof(TDto) != typeof(Building))
            {
                throw new NotSupportedException("Test fake only supports Building DTOs.");
            }

            return Task.FromResult((IReadOnlyList<TDto>)(object)GetAsyncResult);
        }

        public IAsyncEnumerable<TDto> GetPaginatedAsync<TDto>(string endpoint, Action<PythagorasQuery<TDto>>? configure, int pageSize, CancellationToken cancellationToken) where TDto : class
        {
            GetPaginatedAsyncCalled = true;
            LastEndpoint = endpoint;
            LastConfigure = configure;
            LastCancellationToken = cancellationToken;
            LastPageSize = pageSize;

            if (typeof(TDto) != typeof(Building))
            {
                throw new NotSupportedException("Test fake only supports Building DTOs.");
            }

            return (IAsyncEnumerable<TDto>)(object)GetPaginatedAsyncResult;
        }
    }

    private static IEnumerable<T> Empty<T>()
    {
        yield break;
    }

    private static async IAsyncEnumerable<T> YieldAsync<T>(params T[] items)
    {
        foreach (T item in items)
        {
            yield return item;
            await Task.Yield();
        }
    }
}

public static class AsyncEnumerableHelper
{
    public static async IAsyncEnumerable<T> Empty<T>()
    {
        await Task.CompletedTask;

        yield break;
    }
}
