using Umea.se.EstateService.API.Controllers;
using Umea.se.EstateService.Logic.Handlers;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Test.Pythagoras;

public class BuildingControllerTests
{
    [Fact]
    public async Task GetBuildingsAsync_ReturnsFirst50Buildings()
    {
        FakePythagorasClient client = new()
        {
            GetAsyncResult =
            [
                new() { Id = 1, Name = "One" },
                new() { Id = 2, Name = "Two" }
            ]
        };

        PythagorasHandler service = new(client);
        BuildingController controller = new(service);

        IReadOnlyList<BuildingInfoModel> buildings = await controller.GetBuildingsAsync(CancellationToken.None);

        buildings.Select(b => b.Id).ShouldBe(new[] { 1, 2 });
        client.LastQueryString.ShouldBe("maxResults=50");
        client.LastEndpoint.ShouldBe("rest/v1/building/info");
    }

    [Fact]
    public async Task GetBuildingsContainingAsync_FiltersByNameContains()
    {
        FakePythagorasClient client = new()
        {
            GetAsyncResult =
            [
                new() { Id = 3, Name = "Alpha" }
            ]
        };

        PythagorasHandler service = new(client);
        BuildingController controller = new(service);

        IReadOnlyList<BuildingInfoModel> buildings = await controller.GetBuildingsContainingAsync("alp", CancellationToken.None);

        BuildingInfoModel building = buildings.ShouldHaveSingleItem();
        building.Name.ShouldBe("Alpha");

        Dictionary<string, List<string>> query = Parse(client.LastQueryString);

        string parameterName = query["pN[]"].ShouldHaveSingleItem();
        parameterName.ShouldBe("ILIKEAW:name");
        string parameterValue = query["pV[]"].ShouldHaveSingleItem();
        parameterValue.ShouldBe("alp");
        client.LastEndpoint.ShouldBe("rest/v1/building/info");
    }

    private static Dictionary<string, List<string>> Parse(string? query)
    {
        Dictionary<string, List<string>> result = new(StringComparer.Ordinal);

        if (string.IsNullOrEmpty(query))
        {
            return result;
        }

        string[] pairs = query.Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (string pair in pairs)
        {
            string[] kv = pair.Split('=', 2);
            string key = Uri.UnescapeDataString(kv[0]);
            string value = kv.Length > 1 ? Uri.UnescapeDataString(kv[1]) : string.Empty;

            if (!result.TryGetValue(key, out List<string>? list))
            {
                list = [];
                result[key] = list;
            }

            list.Add(value);
        }

        return result;
    }

    [Fact]
    public async Task GetBuildingRoomsAsync_ReturnsMappedRooms()
    {
        FakePythagorasClient client = new()
        {
            GetBuildingWorkspacesResult =
            [
                new() { Id = 10, BuildingId = 1, BuildingName = "B" }
            ]
        };

        PythagorasHandler service = new(client);
        BuildingController controller = new(service);

        IReadOnlyList<BuildingRoomModel> result = await controller.GetBuildingRoomsAsync(1, CancellationToken.None);

        BuildingRoomModel room = result.ShouldHaveSingleItem();
        room.Id.ShouldBe(10);
        client.LastEndpoint.ShouldBe("rest/v1/building/1/workspace/info");
    }

    private sealed class FakePythagorasClient : IPythagorasClient
    {
        public IReadOnlyList<BuildingInfo> GetAsyncResult { get; set; } = [];
        public IReadOnlyList<BuildingWorkspace> GetBuildingWorkspacesResult { get; set; } = [];
        public string? LastQueryString { get; private set; }

        public string? LastEndpoint { get; private set; }

        public Task<IReadOnlyList<TDto>> GetAsync<TDto>(string endpoint, PythagorasQuery<TDto>? query, CancellationToken cancellationToken) where TDto : class
        {
            if (typeof(TDto) != typeof(BuildingInfo))
            {
                if (typeof(TDto) == typeof(BuildingWorkspace))
                {
                    LastEndpoint = endpoint;
                    return Task.FromResult((IReadOnlyList<TDto>)(object)GetBuildingWorkspacesResult);
                }

                throw new NotSupportedException("Test fake only supports Building and BuildingWorkspace DTOs.");
            }

            LastEndpoint = endpoint;
            LastQueryString = query?.BuildAsQueryString();

            return Task.FromResult((IReadOnlyList<TDto>)(object)GetAsyncResult);
        }

        public IAsyncEnumerable<TDto> GetPaginatedAsync<TDto>(string endpoint, PythagorasQuery<TDto>? query, int pageSize, CancellationToken cancellationToken) where TDto : class
            => throw new NotSupportedException();
    }
}
