using Umea.se.EstateService.API.Controllers;
using Umea.se.EstateService.Logic.Handlers;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Test.Pythagoras;

public class EstateControllerTests
{
    [Fact]
    public async Task GetEstateBuildingsAsync_ReturnsFilteredBuildingsByEstateId()
    {
        FakePythagorasClient client = new()
        {
            GetAsyncResult =
            [
                new() { Id = 10, Name = "Building A" },
                new() { Id = 11, Name = "Building B" }
            ]
        };

        PythagorasHandler service = new(client);
        EstateController controller = new(service);

        IReadOnlyList<BuildingInfoModel> buildings = await controller.GetEstateBuildingsAsync(123, CancellationToken.None);

        buildings.Count.ShouldBe(2);
        buildings.Select(b => b.Id).ShouldBe(new[] { 10, 11 });
        client.LastQueryString.ShouldNotBeNull();
        client.LastQueryString.ShouldContain("navigationFolderId=123");
        client.LastEndpoint.ShouldBe("rest/v1/building");
    }

    [Fact]
    public async Task GetEstateBuildingsAsync_WithZeroResults_ReturnsEmptyList()
    {
        FakePythagorasClient client = new()
        {
            GetAsyncResult = []
        };

        PythagorasHandler service = new(client);
        EstateController controller = new(service);

        IReadOnlyList<BuildingInfoModel> buildings = await controller.GetEstateBuildingsAsync(456, CancellationToken.None);

        buildings.ShouldBeEmpty();
        client.LastQueryString.ShouldNotBeNull();
        client.LastQueryString.ShouldContain("navigationFolderId=456");
        client.LastEndpoint.ShouldBe("rest/v1/building");
    }

    private sealed class FakePythagorasClient : IPythagorasClient
    {
        public IReadOnlyList<Building> GetAsyncResult { get; set; } = [];
        public string? LastQueryString { get; private set; }
        public string? LastEndpoint { get; private set; }

        public Task<IReadOnlyList<TDto>> GetAsync<TDto>(string endpoint, PythagorasQuery<TDto>? query, CancellationToken cancellationToken) where TDto : class
        {
            if (typeof(TDto) != typeof(Building))
            {
                throw new NotSupportedException("Test fake only supports Building DTOs.");
            }

            LastEndpoint = endpoint;
            LastQueryString = query?.BuildAsQueryString();

            return Task.FromResult((IReadOnlyList<TDto>)(object)GetAsyncResult);
        }

        public IAsyncEnumerable<TDto> GetPaginatedAsync<TDto>(string endpoint, PythagorasQuery<TDto>? query, int pageSize, CancellationToken cancellationToken) where TDto : class
            => throw new NotSupportedException();
    }
}
