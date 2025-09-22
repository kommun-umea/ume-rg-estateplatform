using Microsoft.AspNetCore.Mvc;
using Umea.se.EstateService.API.Controllers;
using Umea.se.EstateService.Logic.Handlers;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Test.Pythagoras;

public class WorkspaceControllerTests
{
    [Fact]
    public async Task GetWorkspacesAsync_ReturnsWorkspaces()
    {
        FakePythagorasClient client = new()
        {
            GetWorkspacesResult =
            [
                new() { Id = 11, Name = "WS" }
            ]
        };

        PythagorasHandler service = new(client);
        WorkspaceController controller = new(service);

        ActionResult<IReadOnlyList<WorkspaceModel>> response = await controller.GetWorkspacesAsync(null, null, null, CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(response.Result);
        IReadOnlyList<WorkspaceModel> workspaces = Assert.IsAssignableFrom<IReadOnlyList<WorkspaceModel>>(ok.Value);
        WorkspaceModel workspace = Assert.Single(workspaces);
        Assert.Equal(11, workspace.Id);
        Assert.Equal("rest/v1/workspace", client.LastEndpoint);
    }

    [Fact]
    public async Task GetWorkspacesAsync_WithIdsAndSearch_ReturnsBadRequest()
    {
        FakePythagorasClient client = new();
        PythagorasHandler service = new(client);
        WorkspaceController controller = new(service);

        ActionResult<IReadOnlyList<WorkspaceModel>> response = await controller.GetWorkspacesAsync([1], "foo", null, CancellationToken.None);

        BadRequestObjectResult badRequest = Assert.IsType<BadRequestObjectResult>(response.Result);
        Assert.Equal("Specify either ids or generalSearch, not both.", badRequest.Value);
    }

    private sealed class FakePythagorasClient : IPythagorasClient
    {
        public IReadOnlyList<Building> GetAsyncResult { get; set; } = [];
        public IReadOnlyList<Workspace> GetWorkspacesResult { get; set; } = [];
        public string? LastEndpoint { get; private set; }

        public Task<IReadOnlyList<TDto>> GetAsync<TDto>(string endpoint, PythagorasQuery<TDto>? query, CancellationToken cancellationToken) where TDto : class
        {
            LastEndpoint = endpoint;

            if (typeof(TDto) == typeof(Workspace))
            {
                return Task.FromResult((IReadOnlyList<TDto>)(object)GetWorkspacesResult);
            }

            if (typeof(TDto) == typeof(Building))
            {
                return Task.FromResult((IReadOnlyList<TDto>)(object)GetAsyncResult);
            }

            throw new NotSupportedException();
        }

        public IAsyncEnumerable<TDto> GetPaginatedAsync<TDto>(string endpoint, PythagorasQuery<TDto>? query, int pageSize, CancellationToken cancellationToken) where TDto : class
            => throw new NotSupportedException();

        public Task<IReadOnlyList<TDto>> GetOldAsync<TDto>(string endpoint, Action<PythagorasQuery<TDto>>? configure, CancellationToken cancellationToken) where TDto : class
        {
            PythagorasQuery<TDto>? builder = null;
            if (configure is not null)
            {
                builder = new PythagorasQuery<TDto>();
                configure(builder);
            }

            return GetAsync(endpoint, builder, cancellationToken);
        }

        public IAsyncEnumerable<TDto> GetOldPaginatedAsync<TDto>(string endpoint, Action<PythagorasQuery<TDto>>? configure, int pageSize, CancellationToken cancellationToken) where TDto : class
            => throw new NotSupportedException();
    }
}
