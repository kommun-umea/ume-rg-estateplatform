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
        client.LastEndpoint.ShouldBe("rest/v1/building/info");
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
        client.LastEndpoint.ShouldBe("rest/v1/building/info");
    }

    private sealed class FakePythagorasClient : IPythagorasClient
    {
        public IReadOnlyList<BuildingInfo> GetAsyncResult { get; set; } = [];
        public string? LastQueryString { get; private set; }
        public string? LastEndpoint { get; private set; }

        public Task<IReadOnlyList<TDto>> GetAsync<TDto>(string endpoint, PythagorasQuery<TDto>? query, CancellationToken cancellationToken) where TDto : class, IPythagorasDto
        {
            LastEndpoint = endpoint;
            LastQueryString = query?.BuildAsQueryString();

            if (typeof(TDto) == typeof(Building))
            {
                return Task.FromResult((IReadOnlyList<TDto>)(object)GetAsyncResult);
            }
            if (typeof(TDto) == typeof(BuildingInfoModel))
            {
                List<BuildingInfoModel> mapped = GetAsyncResult
                    .Select(b => new BuildingInfoModel
                    {
                        Id = b.Id,
                        Uid = b.Uid,
                        Name = b.Name,
                        PopularName = b.PopularName,
                        GeoLocation = null,
                        // The rest are not present in Building, so use defaults:
                        GrossArea = 0,
                        NetArea = 0,
                        SumGrossFloorArea = 0,
                        NumPlacedPersons = 0,
                        Address = null,
                        ExtraInfo = new Dictionary<string, string?>(),
                        PropertyValues = new Dictionary<string, string?>(),
                        NavigationInfo = new Dictionary<string, string?>()
                    })
                    .ToList();
                return Task.FromResult((IReadOnlyList<TDto>)(object)mapped);
            }
            if (typeof(TDto) == typeof(BuildingInfo))
            {
                List<BuildingInfo> mapped = GetAsyncResult
                    .Select(b => new BuildingInfo
                    {
                        Id = b.Id,
                        Uid = b.Uid,
                        Name = b.Name,
                        PopularName = b.PopularName,
                        MarkerType = b.MarkerType,
                        Grossarea = null,
                        Netarea = null,
                        SumGrossFloorarea = null,
                        NumPlacedPersons = 0,
                        GeoX = b.GeoX,
                        GeoY = b.GeoY,
                        GeoRotation = b.GeoRotation,
                        AddressName = string.Empty,
                        AddressCity = string.Empty,
                        AddressCountry = string.Empty,
                        AddressStreet = string.Empty,
                        AddressZipCode = string.Empty,
                        AddressExtra = string.Empty,
                        Origin = b.Origin,
                        CurrencyId = null,
                        CurrencyName = null,
                        FlagStatusIds = [],
                        BusinessTypeId = null,
                        BusinessTypeName = null,
                        ProspectOfBuildingId = null,
                        IsProspect = false,
                        ProspectStartDate = null,
                        ExtraInfo = [],
                        PropertyValues = [],
                        NavigationInfo = []
                    })
                    .ToList();
                return Task.FromResult((IReadOnlyList<TDto>)(object)mapped);
            }

            throw new NotSupportedException("Test fake only supports Building, BuildingInfo, and BuildingInfoModel DTOs.");
        }

        public IAsyncEnumerable<TDto> GetPaginatedAsync<TDto>(string endpoint, PythagorasQuery<TDto>? query, int pageSize, CancellationToken cancellationToken) where TDto : class, IPythagorasDto
            => throw new NotSupportedException();
    }
}
