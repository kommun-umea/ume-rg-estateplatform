using Microsoft.AspNetCore.Mvc;
using Umea.se.EstateService.API.Controllers;
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
        RoomController controller = new(service);

        ActionResult<IReadOnlyList<RoomModel>> response = await controller.GetRoomsAsync(null, null, null, CancellationToken.None);

        OkObjectResult ok = response.Result.ShouldBeOfType<OkObjectResult>();
        object obj = ok.Value.ShouldNotBeNull();
        IReadOnlyList<RoomModel>? rooms = obj.ShouldBeAssignableTo<IReadOnlyList<RoomModel>>();
        RoomModel room = rooms.ShouldHaveSingleItem();
        room.Id.ShouldBe(11);
        client.LastEndpoint.ShouldBe("rest/v1/workspace/info");
    }

    [Fact]
    public async Task GetRoomsAsync_WithIdsAndSearch_ReturnsBadRequest()
    {
        FakePythagorasClient client = new();
        PythagorasHandler service = new(client);
        RoomController controller = new(service);

        ActionResult<IReadOnlyList<RoomModel>> response = await controller.GetRoomsAsync([1], "foo", null, CancellationToken.None);

        BadRequestObjectResult badRequest = response.Result.ShouldBeOfType<BadRequestObjectResult>();
        badRequest.Value.ShouldBe("Specify either ids or generalSearch, not both.");
    }

    private sealed class FakePythagorasClient : IPythagorasClient
    {
        public string? LastEndpoint { get; private set; }

        public IReadOnlyList<Workspace> GetWorkspacesResult { get; set; } = [];

        public Task<IReadOnlyList<T>> GetAsync<T>(string endpoint, PythagorasQuery<T>? query, CancellationToken cancellationToken) where T : class
        {
            LastEndpoint = endpoint;

            if (typeof(T) == typeof(Workspace))
            {
                return Task.FromResult((IReadOnlyList<T>)(object)GetWorkspacesResult);
            }

            return Task.FromResult<IReadOnlyList<T>>([]);
        }
    }
}
