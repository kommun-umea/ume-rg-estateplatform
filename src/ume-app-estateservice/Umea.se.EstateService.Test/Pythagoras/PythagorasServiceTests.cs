using Umea.se.EstateService.Logic.Handlers;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enum;
using Umea.se.EstateService.Shared.Models;
using Umea.se.EstateService.Shared.ValueObjects;

namespace Umea.se.EstateService.Test.Pythagoras;

public class PythagorasHandlerTests
{
    [Fact]
    public async Task GetBuildingsAsync_DelegatesToClient()
    {
        FakePythagorasClient client = new()
        {
            GetBuildingInfoResult = [new() { Id = 42 }]
        };
        PythagorasHandler service = new(client);
        using CancellationTokenSource cts = new();

        PythagorasQuery<BuildingInfo> query = new PythagorasQuery<BuildingInfo>()
            .WithIds(42);
        IReadOnlyList<BuildingInfoModel> result = await service.GetBuildingsAsync(query, cts.Token);

        client.GetAsyncCalled.ShouldBeTrue();
        client.LastEndpoint.ShouldBe("rest/v1/building/info");
        client.LastQuery.ShouldBeSameAs(query);
        client.LastCancellationToken.ShouldBe(cts.Token);
        BuildingInfoModel model = result.ShouldHaveSingleItem();
        model.Id.ShouldBe(42);
    }

    [Fact]
    public async Task GetBuildingInfoAsync_DelegatesToClientAndAddsNavigationFolder()
    {
        Guid uid = Guid.NewGuid();
        FakePythagorasClient client = new()
        {
            GetBuildingInfoResult =
            [
                new()
                {
                    Id = 10,
                    Uid = uid,
                    Name = "Info",
                    PopularName = "Info Popular",
                    Grossarea = 12.5m,
                    Netarea = 10.2m,
                    SumGrossFloorarea = 13.4m,
                    NumPlacedPersons = 3,
                    GeoX = 1,
                    GeoY = 2,
                    GeoRotation = 3,
                    AddressStreet = "Street",
                    AddressZipCode = "Zip",
                    AddressCity = "City",
                    AddressCountry = "Country",
                    MarkerType = PythMarkerType.Unknown
                }
            ]
        };

        PythagorasHandler service = new(client);

        IReadOnlyList<BuildingInfoModel> result = await service.GetBuildingInfoAsync(navigationFolderId: 1234);

        client.GetAsyncCalled.ShouldBeTrue();
        client.LastEndpoint.ShouldBe("rest/v1/building/info");
        client.LastQuery.ShouldNotBeNull();
        PythagorasQuery<BuildingInfo> query = client.LastQuery.ShouldBeOfType<PythagorasQuery<BuildingInfo>>();
        string queryString = query.BuildAsQueryString();
        queryString.ShouldContain("navigationFolderId=1234");

        BuildingInfoModel model = result.ShouldHaveSingleItem();
        model.Id.ShouldBe(10);
        model.Uid.ShouldBe(uid);
        model.GrossArea.ShouldBe(12.5m);
        model.NetArea.ShouldBe(10.2m);
        model.SumGrossFloorArea.ShouldBe(13.4m);
        model.NumPlacedPersons.ShouldBe(3);
        model.GeoLocation.ShouldNotBeNull();
        model.Address.ShouldNotBe(AddressModel.Empty);
    }

    [Fact]
    public async Task GetBuildingInfoAsync_WithoutNavigationFolder_DoesNotAddFilter()
    {
        FakePythagorasClient client = new()
        {
            GetBuildingInfoResult = [new() { Id = 1 }]
        };

        PythagorasHandler service = new(client);

        IReadOnlyList<BuildingInfoModel> result = await service.GetBuildingInfoAsync();

        client.GetAsyncCalled.ShouldBeTrue();
        client.LastEndpoint.ShouldBe("rest/v1/building/info");
        client.LastQuery.ShouldNotBeNull();
        PythagorasQuery<BuildingInfo> query = client.LastQuery.ShouldBeOfType<PythagorasQuery<BuildingInfo>>();
        string queryString = query.BuildAsQueryString();
        queryString.ShouldNotContain("navigationFolderId=");
        result.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task GetBuildingWorkspacesAsync_DelegatesToClient()
    {
        FakePythagorasClient client = new()
        {
            GetBuildingWorkspacesResult = [new() { Id = 5, BuildingId = 99, BuildingName = "B" }]
        };

        PythagorasHandler service = new(client);

        IReadOnlyList<BuildingRoomModel> result = await service.GetBuildingWorkspacesAsync(99);

        client.GetAsyncCalled.ShouldBeTrue();
        client.LastEndpoint.ShouldBe("rest/v1/building/99/workspace/info");
        BuildingRoomModel room = result.ShouldHaveSingleItem();
        room.Id.ShouldBe(5);
    }

    [Fact]
    public async Task GetWorkspacesAsync_DelegatesToClient()
    {
        FakePythagorasClient client = new()
        {
            GetWorkspacesResult = [new() { Id = 7, Name = "W" }]
        };

        PythagorasHandler service = new(client);

        IReadOnlyList<RoomModel> result = await service.GetRoomsAsync();

        client.GetAsyncCalled.ShouldBeTrue();
        client.LastEndpoint.ShouldBe("rest/v1/workspace/info");
        client.LastQuery.ShouldBeNull();
        RoomModel room = result.ShouldHaveSingleItem();
        room.Id.ShouldBe(7);
    }

    private sealed class FakePythagorasClient : IPythagorasClient
    {
        public object? LastQuery { get; private set; }
        public CancellationToken LastCancellationToken { get; private set; }
        public bool GetAsyncCalled { get; private set; }
        public bool GetPaginatedAsyncCalled { get; private set; }
        public int LastPageSize { get; private set; }
        public IReadOnlyList<Building> GetAsyncResult { get; set; } = [];
        public IReadOnlyList<BuildingInfo> GetBuildingInfoResult { get; set; } = [];
        public IReadOnlyList<BuildingWorkspace> GetBuildingWorkspacesResult { get; set; } = [];
        public IReadOnlyList<Workspace> GetWorkspacesResult { get; set; } = [];
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

            if (typeof(TDto) == typeof(BuildingInfo))
            {
                return Task.FromResult((IReadOnlyList<TDto>)(object)GetBuildingInfoResult);
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
    }
}
