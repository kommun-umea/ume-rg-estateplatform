using Umea.se.EstateService.Logic.Handlers;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Models;
using Umea.se.EstateService.Test.Helpers;

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

        PythagorasQuery<Building> query = new PythagorasQuery<Building>()
            .WithIds(42);
        IReadOnlyList<BuildingModel> result = await service.GetBuildingsAsync(query, cts.Token);

        client.GetAsyncCalled.ShouldBeTrue();
        client.LastEndpoint.ShouldBe("rest/v1/building");
        client.LastQuery.ShouldBeSameAs(query);
        client.LastCancellationToken.ShouldBe(cts.Token);
        BuildingModel model = result.ShouldHaveSingleItem();
        model.Id.ShouldBe(42);
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

        client.GetPaginatedAsyncCalled.ShouldBeTrue();
        client.LastEndpoint.ShouldBe("rest/v1/building");
        client.LastQuery.ShouldBeNull();
        client.LastPageSize.ShouldBe(10);
        client.LastCancellationToken.ShouldBe(cts.Token);
        collected.Select(b => b.Id).ShouldBe(new[] { 1, 2 });
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

        client.GetAsyncCalled.ShouldBeTrue();
        client.LastEndpoint.ShouldBe("rest/v1/building/99/workspace/info");
        BuildingWorkspaceModel workspace = result.ShouldHaveSingleItem();
        workspace.Id.ShouldBe(5);
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

        client.GetAsyncCalled.ShouldBeTrue();
        client.LastEndpoint.ShouldBe("rest/v1/workspace");
        client.LastQuery.ShouldBeNull();
        WorkspaceModel workspace = result.ShouldHaveSingleItem();
        workspace.Id.ShouldBe(7);
    }

    private sealed class FakePythagorasClient : IPythagorasClient
    {
        public object? LastQuery { get; private set; }
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

        public Task<IReadOnlyList<TDto>> GetAsync<TDto>(string endpoint, PythagorasQuery<TDto>? query, CancellationToken cancellationToken) where TDto : class
        {
            GetAsyncCalled = true;
            LastEndpoint = endpoint;
            LastCancellationToken = cancellationToken;
            LastQuery = query;

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

        public IAsyncEnumerable<TDto> GetPaginatedAsync<TDto>(string endpoint, PythagorasQuery<TDto>? query, int pageSize, CancellationToken cancellationToken) where TDto : class
        {
            GetPaginatedAsyncCalled = true;
            LastEndpoint = endpoint;
            LastCancellationToken = cancellationToken;
            LastPageSize = pageSize;
            LastQuery = query;

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

        public Task<IReadOnlyList<TDto>> GetOldAsync<TDto>(string endpoint, Action<PythagorasQuery<TDto>>? configure, CancellationToken cancellationToken) where TDto : class
        {
            PythagorasQuery<TDto>? query = null;
            if (configure is not null)
            {
                query = new PythagorasQuery<TDto>();
                configure(query);
            }

            return GetAsync(endpoint, query, cancellationToken);
        }

        public IAsyncEnumerable<TDto> GetOldPaginatedAsync<TDto>(string endpoint, Action<PythagorasQuery<TDto>>? configure, int pageSize, CancellationToken cancellationToken) where TDto : class
        {
            PythagorasQuery<TDto>? query = null;
            if (configure is not null)
            {
                query = new PythagorasQuery<TDto>();
                configure(query);
            }

            return GetPaginatedAsync(endpoint, query, pageSize, cancellationToken);
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
