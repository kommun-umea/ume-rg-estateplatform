using Umea.se.EstateService.Logic.Handlers;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Test.Pythagoras;

public class PythagorasHandlerTests
{
    [Fact]
    public async Task GetBuildingsAsync_DelegatesToClient()
    {
        FakePythagorasClient client = new()
        {
            GetAsyncResult = [new() { Id = 42 }]
        };
        PythagorasHandler service = new(client);
        using CancellationTokenSource cts = new();

        Action<PythagorasQuery<Building>> configure = query => query.WithIds(42);
        IReadOnlyList<BuildingModel> result = await service.GetBuildingsAsync(configure, cts.Token);

        Assert.True(client.GetAsyncCalled);
        Assert.Equal("rest/v1/building", client.LastEndpoint);
        Assert.Same(configure, client.LastConfigure);
        Assert.Equal(cts.Token, client.LastCancellationToken);
        BuildingModel model = Assert.Single(result);
        Assert.Equal(42, model.Id);
    }

    [Fact]
    public async Task GetAllBuildingsAsync_DelegatesToClient()
    {
        FakePythagorasClient client = new()
        {
            GetPaginatedAsyncResult = YieldAsync(new Building { Id = 1 }, new Building { Id = 2 })
        };
        PythagorasHandler service = new(client);
        using CancellationTokenSource cts = new();

        List<BuildingModel> collected = [];
        await foreach (BuildingModel building in service.GetPaginatedBuildingsAsync(null, pageSize: 10, cts.Token))
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

    [Fact]
    public async Task GetBuildingWorkspacesAsync_DelegatesToClient()
    {
        FakePythagorasClient client = new()
        {
            GetBuildingWorkspacesResult = [new() { Id = 5, BuildingId = 99, BuildingName = "B" }]
        };

        PythagorasHandler service = new(client);

        IReadOnlyList<BuildingWorkspaceModel> result = await service.GetBuildingWorkspacesAsync(99);

        Assert.True(client.GetAsyncCalled);
        Assert.Equal("rest/v1/building/99/workspace/info", client.LastEndpoint);
        BuildingWorkspaceModel workspace = Assert.Single(result);
        Assert.Equal(5, workspace.Id);
    }

    [Fact]
    public async Task GetWorkspacesAsync_DelegatesToClient()
    {
        FakePythagorasClient client = new()
        {
            GetWorkspacesResult = [new() { Id = 7, Name = "W" }]
        };

        PythagorasHandler service = new(client);

        IReadOnlyList<WorkspaceModel> result = await service.GetWorkspacesAsync();

        Assert.True(client.GetAsyncCalled);
        Assert.Equal("rest/v1/workspace", client.LastEndpoint);
        WorkspaceModel workspace = Assert.Single(result);
        Assert.Equal(7, workspace.Id);
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
        public IReadOnlyList<BuildingWorkspace> GetBuildingWorkspacesResult { get; set; } = [];
        public IReadOnlyList<Workspace> GetWorkspacesResult { get; set; } = [];
        public IAsyncEnumerable<Workspace> GetPaginatedWorkspacesResult { get; set; } = AsyncEnumerableHelper.Empty<Workspace>();
        public string? LastEndpoint { get; private set; }

        public Task<IReadOnlyList<TDto>> GetAsync<TDto>(string endpoint, Action<PythagorasQuery<TDto>>? configure, CancellationToken cancellationToken) where TDto : class
        {
            GetAsyncCalled = true;
            LastEndpoint = endpoint;
            LastConfigure = configure;
            LastCancellationToken = cancellationToken;

            if (typeof(TDto) == typeof(Building))
            {
                return Task.FromResult((IReadOnlyList<TDto>)(object)GetAsyncResult);
            }

            if (typeof(TDto) == typeof(BuildingWorkspace))
            {
                return Task.FromResult((IReadOnlyList<TDto>)(object)GetBuildingWorkspacesResult);
            }

            if (typeof(TDto) == typeof(Workspace))
            {
                return Task.FromResult((IReadOnlyList<TDto>)(object)GetWorkspacesResult);
            }

            throw new NotSupportedException("Test fake does not support the requested DTO type.");
        }

        public IAsyncEnumerable<TDto> GetPaginatedAsync<TDto>(string endpoint, Action<PythagorasQuery<TDto>>? configure, int pageSize, CancellationToken cancellationToken) where TDto : class
        {
            GetPaginatedAsyncCalled = true;
            LastEndpoint = endpoint;
            LastConfigure = configure;
            LastCancellationToken = cancellationToken;
            LastPageSize = pageSize;

            if (typeof(TDto) == typeof(Building))
            {
                return (IAsyncEnumerable<TDto>)(object)GetPaginatedAsyncResult;
            }

            if (typeof(TDto) == typeof(Workspace))
            {
                return (IAsyncEnumerable<TDto>)(object)GetPaginatedWorkspacesResult;
            }

            throw new NotSupportedException("Test fake does not support the requested DTO type.");
        }
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
