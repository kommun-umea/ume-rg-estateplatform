using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umea.se.EstateService.API.Controllers;
using Umea.se.EstateService.API.Controllers.Requests;
using Umea.se.EstateService.Logic.Handlers;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Test.Pythagoras;

public class RoomControllerTests
{
    [Fact]
    public async Task GetRoomsAsync_ReturnsRooms()
    {
        FakePythagorasClient client = new()
        {
            GetWorkspacesResult =
            [
                new() { Id = 11, Name = "WS" }
            ]
        };

        PythagorasHandler service = new(client);
        RoomController controller = CreateController(service);

        ActionResult<IReadOnlyList<RoomModel>> response = await controller.GetRoomsAsync(new RoomListRequest(), CancellationToken.None);

        OkObjectResult ok = response.Result.ShouldBeOfType<OkObjectResult>();
        IReadOnlyList<RoomModel>? rooms = ok.Value.ShouldBeAssignableTo<IReadOnlyList<RoomModel>>();
        rooms.ShouldHaveSingleItem().Id.ShouldBe(11);
        client.LastEndpoint.ShouldBe("rest/v1/workspace/info");
        client.LastQueryString.ShouldBe("maxResults=50");
    }

    [Fact]
    public async Task GetRoomsAsync_WithIds_BuildsIdsOnlyQuery()
    {
        FakePythagorasClient client = new()
        {
            GetWorkspacesResult =
            [
                new() { Id = 11, Name = "WS" }
            ]
        };

        PythagorasHandler service = new(client);
        RoomController controller = CreateController(service);

        ActionResult<IReadOnlyList<RoomModel>> response = await controller.GetRoomsAsync(
            new RoomListRequest { Ids = new[] { 1, 2 } },
            CancellationToken.None);

        _ = response.Result.ShouldBeOfType<OkObjectResult>();
        client.LastQueryString.ShouldNotBeNull();
        string decodedQuery = Uri.UnescapeDataString(client.LastQueryString ?? string.Empty);
        decodedQuery.ShouldContain("ids[]=1");
        decodedQuery.ShouldContain("ids[]=2");
        decodedQuery.ShouldNotContain("generalSearch");
    }

    [Fact]
    public async Task GetRoomsAsync_WithSearchAndBuildingId_AppliesFilters()
    {
        FakePythagorasClient client = new()
        {
            GetWorkspacesResult =
            [
                new() { Id = 11, Name = "WS" }
            ]
        };

        PythagorasHandler service = new(client);
        RoomController controller = CreateController(service);

        RoomListRequest request = new()
        {
            SearchTerm = "lab",
            BuildingId = 42,
            Limit = 10,
            Offset = 5
        };

        ActionResult<IReadOnlyList<RoomModel>> response = await controller.GetRoomsAsync(request, CancellationToken.None);

        _ = response.Result.ShouldBeOfType<OkObjectResult>();
        client.LastQueryString.ShouldNotBeNull();
        client.LastQueryString.ShouldContain("generalSearch=lab");
        client.LastQueryString.ShouldContain("firstResult=5");
        client.LastQueryString.ShouldContain("maxResults=10");
        client.LastQueryString.ShouldContain("buildingId=42");
    }

    [Fact]
    public async Task GetRoomsAsync_WithIdsAndSearch_ReturnsValidationProblem()
    {
        FakePythagorasClient client = new();
        PythagorasHandler service = new(client);
        RoomController controller = CreateController(service);

        ActionResult<IReadOnlyList<RoomModel>> response = await controller.GetRoomsAsync(
            new RoomListRequest { Ids = new[] { 1 }, SearchTerm = "foo" },
            CancellationToken.None);

        ObjectResult problem = response.Result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        problem.Value.ShouldBeOfType<ValidationProblemDetails>()
            .Errors.Keys.ShouldContain(nameof(RoomListRequest.Ids));
    }

    private sealed class FakePythagorasClient : IPythagorasClient
    {
        public string? LastEndpoint { get; private set; }
        public string? LastQueryString { get; private set; }

        public IReadOnlyList<Workspace> GetWorkspacesResult { get; set; } = [];

        public Task<IReadOnlyList<T>> GetAsync<T>(string endpoint, PythagorasQuery<T>? query, CancellationToken cancellationToken) where T : class, IPythagorasDto
        {
            LastEndpoint = endpoint;
            LastQueryString = query?.BuildAsQueryString();

            if (typeof(T) == typeof(Workspace))
            {
                return Task.FromResult((IReadOnlyList<T>)(object)GetWorkspacesResult);
            }

            return Task.FromResult<IReadOnlyList<T>>([]);
        }

        public IAsyncEnumerable<T> GetPaginatedAsync<T>(string endpoint, PythagorasQuery<T>? query, int pageSize, CancellationToken cancellationToken) where T : class, IPythagorasDto
            => throw new NotSupportedException();
    }

    private static RoomController CreateController(PythagorasHandler service)
    {
        RoomController controller = new(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        return controller;
    }
}
