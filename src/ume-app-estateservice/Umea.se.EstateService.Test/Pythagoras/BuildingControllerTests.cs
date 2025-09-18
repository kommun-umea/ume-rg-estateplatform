using Umea.se.EstateService.API.Controllers;
using Umea.se.EstateService.ServiceAccess.Pythagoras;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Pythagoras;

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

        PythagorasService service = new(client);
        BuildingController controller = new(service);

        IReadOnlyList<BuildingModel> buildings = await controller.GetBuildingsAsync(CancellationToken.None);

        Assert.Equal([1, 2], [.. buildings.Select(b => b.Id)]);
        Assert.Equal("maxResults=50", client.LastQueryString);
        Assert.Equal("rest/v1/building", client.LastEndpoint);
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

        PythagorasService service = new(client);
        BuildingController controller = new(service);

        IReadOnlyList<BuildingModel> buildings = await controller.GetBuildingsContainingAsync("alp", CancellationToken.None);

        Assert.Single(buildings);
        Assert.Equal("Alpha", buildings[0].Name);

        Dictionary<string, List<string>> query = Parse(client.LastQueryString);

        Assert.Equal("ILIKEAW:name", Assert.Single(query["pN[]"]));
        Assert.Equal("alp", Assert.Single(query["pV[]"]));
        Assert.Equal("rest/v1/building", client.LastEndpoint);
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

    private sealed class FakePythagorasClient : IPythagorasClient
    {
        public IReadOnlyList<Building> GetAsyncResult { get; set; } = [];
        public string? LastQueryString { get; private set; }

        public string? LastEndpoint { get; private set; }

        public Task<IReadOnlyList<TDto>> GetAsync<TDto>(string endpoint, Action<PythagorasQuery<TDto>>? query, CancellationToken cancellationToken) where TDto : class
        {
            if (typeof(TDto) != typeof(Building))
            {
                throw new NotSupportedException("Test fake only supports Building DTOs.");
            }

            LastEndpoint = endpoint;
            PythagorasQuery<TDto> realQuery = new();
            query?.Invoke(realQuery);
            LastQueryString = realQuery.BuildAsQueryString();

            return Task.FromResult((IReadOnlyList<TDto>)(object)GetAsyncResult);
        }

        public IAsyncEnumerable<TDto> GetPaginatedAsync<TDto>(string endpoint, Action<PythagorasQuery<TDto>>? configure, int pageSize, CancellationToken cancellationToken) where TDto : class
            => throw new NotSupportedException();
    }
}
