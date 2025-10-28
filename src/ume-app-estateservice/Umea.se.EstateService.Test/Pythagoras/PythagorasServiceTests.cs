using Umea.se.EstateService.Logic.Handlers;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enum;
using Umea.se.EstateService.Shared.Models;
using Umea.se.EstateService.Test.TestHelpers;

namespace Umea.se.EstateService.Test.Pythagoras;

public class PythagorasHandlerTests
{
    [Fact]
    public async Task GetBuildingsAsync_DelegatesToClient()
    {
        FakePythagorasClient client = new();
        client.SetGetAsyncResult(
            new BuildingInfo { Id = 42 });
        PythagorasHandler service = new(client);
        using CancellationTokenSource cts = new();

        PythagorasQuery<BuildingInfo> query = new PythagorasQuery<BuildingInfo>()
            .WithIds(42);
        IReadOnlyList<BuildingInfoModel> result = await service.GetBuildingsAsync(query, cts.Token);

        client.GetAsyncCalled.ShouldBeTrue();
        client.LastEndpoint.ShouldBe("rest/v1/building/info");
        client.LastCancellationToken.ShouldBe(cts.Token);
        string queryString = client.LastQueryString.ShouldNotBeNull();
        queryString.ShouldContain($"navigationId={(int)NavigationType.UmeaKommun}");
        BuildingInfoModel model = result.ShouldHaveSingleItem();
        model.Id.ShouldBe(42);
    }

    [Fact]
    public async Task GetBuildingsAsync_AddsDefaultNavigationIdWhenQueryIsNull()
    {
        FakePythagorasClient client = new();
        client.SetGetAsyncResult(new BuildingInfo { Id = 1 });
        PythagorasHandler service = new(client);

        IReadOnlyList<BuildingInfoModel> result = await service.GetBuildingsAsync();

        result.ShouldHaveSingleItem();
        string queryString = client.LastQueryString.ShouldNotBeNull();
        queryString.ShouldContain($"navigationId={(int)NavigationType.UmeaKommun}");
    }

    [Fact]
    public async Task GetBuildingsAsync_PreservesExistingNavigationFolderParameter()
    {
        FakePythagorasClient client = new();
        client.SetGetAsyncResult(new BuildingInfo { Id = 5 });
        PythagorasHandler service = new(client);

        PythagorasQuery<BuildingInfo> query = new PythagorasQuery<BuildingInfo>()
            .WithQueryParameter("navigationFolderId", 1234);

        await service.GetBuildingsAsync(query);

        client.LastQuery.ShouldNotBeNull();
        string queryString = client.LastQueryString.ShouldNotBeNull();
        queryString.ShouldContain("navigationFolderId=1234");
        queryString.ShouldContain($"navigationId={(int)NavigationType.UmeaKommun}");
    }

    [Fact]
    public async Task GetBuildingsWithPropertiesAsync_UsesUiListDataEndpoint()
    {
        FakePythagorasClient client = new();
        client.SetBuildingUiListDataResponse(new UiListDataResponse<BuildingInfo>
        {
            Data =
            [
                new BuildingInfo
                {
                    Id = 1551,
                    Name = "Building",
                    PropertyValues = new Dictionary<int, PropertyValueDto>
                    {
                        { (int)PropertyCategoryId.YearOfConstruction, new PropertyValueDto { Value = "1982" } }
                    }
                }
            ],
            TotalSize = 1
        });

        PythagorasHandler service = new(client);

        IReadOnlyList<BuildingInfoModel> result = await service.GetBuildingsWithPropertiesAsync([1551]);

        result.ShouldHaveSingleItem().ExtendedProperties?.YearOfConstruction.ShouldBe("1982");

        FakePythagorasClient.BuildingUiListDataRequestCapture request = client.BuildingUiListDataRequests.ShouldHaveSingleItem();
        request.Request.NavigationId.ShouldBe(NavigationType.UmeaKommun);
        request.Request.BuildingIds.ShouldNotBeNull();
        request.Request.BuildingIds!.ShouldContain(1551);
        IReadOnlyCollection<int> propertyIds = request.Request.PropertyIds.ShouldNotBeNull();
        propertyIds.ShouldContain((int)PropertyCategoryId.YearOfConstruction);
    }

    [Fact]
    public async Task GetBuildingsWithPropertiesAsync_UsesProvidedPropertyIdsAndNavigation()
    {
        FakePythagorasClient client = new();
        client.SetBuildingUiListDataResponse(new UiListDataResponse<BuildingInfo>());

        PythagorasHandler service = new(client);

        IReadOnlyList<BuildingInfoModel> result = await service.GetBuildingsWithPropertiesAsync(
            [42],
            propertyIds: [999],
            navigationId: 7);

        result.ShouldBeEmpty();

        FakePythagorasClient.BuildingUiListDataRequestCapture request = client.BuildingUiListDataRequests.ShouldHaveSingleItem();
        request.Request.NavigationId.ShouldBe(7);
        IReadOnlyCollection<int> requestedPropertyIds = request.Request.PropertyIds.ShouldNotBeNull();
        requestedPropertyIds.Count.ShouldBe(1);
        requestedPropertyIds.ShouldContain(999);
    }

    [Fact]
    public async Task GetBuildingsWithPropertiesAsync_WithoutIds_FetchesAll()
    {
        FakePythagorasClient client = new();
        client.SetBuildingUiListDataResponse(new UiListDataResponse<BuildingInfo>());

        PythagorasHandler service = new(client);

        IReadOnlyList<BuildingInfoModel> result = await service.GetBuildingsWithPropertiesAsync();

        result.ShouldBeEmpty();

        FakePythagorasClient.BuildingUiListDataRequestCapture request = client.BuildingUiListDataRequests.ShouldHaveSingleItem();
        request.Request.BuildingIds.ShouldBeNull();
    }

    [Fact]
    public async Task GetBuildingWorkspacesAsync_DelegatesToClient()
    {
        FakePythagorasClient client = new();
        client.SetGetAsyncResult(
            new BuildingWorkspace { Id = 5, BuildingId = 99, BuildingName = "B" });

        PythagorasHandler service = new(client);

        IReadOnlyList<BuildingRoomModel> result = await service.GetBuildingWorkspacesAsync(99);

        client.GetAsyncCalled.ShouldBeTrue();
        client.LastEndpoint.ShouldBe("rest/v1/building/99/workspace/info");
        BuildingRoomModel room = result.ShouldHaveSingleItem();
        room.Id.ShouldBe(5);
    }

    [Fact]
    public async Task GetBuildingFloorsWithRoomsAsync_ReturnsFloorsWithRooms()
    {
        Guid buildingUid = Guid.NewGuid();
        Guid floorZeroUid = Guid.NewGuid();
        Guid floorOneUid = Guid.NewGuid();

        FakePythagorasClient client = new();
        client.SetGetAsyncResult(
            new Floor
            {
                Id = 3022,
                Uid = floorZeroUid,
                Version = 3,
                Created = 1759786556000,
                Updated = 1759826938000,
                Name = "00",
                PopularName = "Ground floor",
                Height = 3,
                ReferenceHeight = -3,
                GrossFloorarea = 3395.81
            },
            new Floor
            {
                Id = 3023,
                Uid = floorOneUid,
                Version = 3,
                Created = 1759786556001,
                Updated = 1759826948000,
                Name = "01",
                PopularName = "First floor",
                Height = 3,
                ReferenceHeight = 0,
                GrossFloorarea = 4372.66
            });

        client.EnqueueGetAsyncResult(
            new BuildingWorkspace
            {
                Id = 1,
                Uid = Guid.NewGuid(),
                Created = 1759786582000,
                Updated = 1759827020000,
                Name = "9-1033",
                BuildingId = 10,
                BuildingUid = buildingUid,
                BuildingName = "MAINBUILDING",
                FloorId = 3022,
                FloorUid = floorZeroUid,
                FloorName = "00"
            });

        client.EnqueueGetAsyncResult(
            new BuildingWorkspace
            {
                Id = 2,
                Uid = Guid.NewGuid(),
                Created = 1759786582000,
                Updated = 1759827020000,
                Name = "9-1042B",
                BuildingId = 10,
                BuildingUid = buildingUid,
                BuildingName = "MAINBUILDING",
                FloorId = 3023,
                FloorUid = floorOneUid,
                FloorName = "01"
            },
            new BuildingWorkspace
            {
                Id = 3,
                Uid = Guid.NewGuid(),
                Created = 1759786582000,
                Updated = 1759827020000,
                Name = "9-1001",
                BuildingId = 10,
                BuildingUid = buildingUid,
                BuildingName = "MAINBUILDING",
                FloorId = 3023,
                FloorUid = floorOneUid,
                FloorName = "01"
            });

        PythagorasHandler service = new(client);

        IReadOnlyList<FloorInfoModel> result = await service.GetBuildingFloorsWithRoomsAsync(10);

        client.EndpointsCalled.ShouldBe(
        [
            "rest/v1/building/10/floor",
            "rest/v1/floor/3022/workspace/info",
            "rest/v1/floor/3023/workspace/info"
        ]);

        result.Count.ShouldBe(2);
        FloorInfoModel floorZero = result[0];
        floorZero.Id.ShouldBe(3022);
        IReadOnlyList<BuildingRoomModel> floorZeroRooms = floorZero.Rooms.ShouldNotBeNull();
        floorZeroRooms.Count.ShouldBe(1);
        floorZeroRooms[0].Name.ShouldBe("9-1033");

        FloorInfoModel floorOne = result[1];
        floorOne.Id.ShouldBe(3023);
        IReadOnlyList<BuildingRoomModel> floorOneRooms = floorOne.Rooms.ShouldNotBeNull();
        floorOneRooms.Count.ShouldBe(2);
        floorOneRooms[0].Name.ShouldBe("9-1042B");
        floorOneRooms[1].Name.ShouldBe("9-1001");
    }

    [Fact]
    public async Task GetBuildingFloorsAsync_ReturnsFloorMetadataOnly()
    {
        Guid buildingUid = Guid.NewGuid();
        Guid floorUid = Guid.NewGuid();

        FakePythagorasClient client = new();
        client.SetGetAsyncResult(
            new Floor
            {
                Id = 400,
                Uid = floorUid,
                BuildingId = 10,
                BuildingUid = buildingUid,
                Name = "04",
                PopularName = "Fourth",
                GrossFloorarea = 1234.56
            });

        PythagorasHandler service = new(client);

        IReadOnlyList<FloorInfoModel> result = await service.GetBuildingFloorsAsync(10);

        client.EndpointsCalled.ShouldBe(["rest/v1/building/10/floor"]);

        FloorInfoModel floor = result.ShouldHaveSingleItem();
        floor.Id.ShouldBe(400);
        floor.Uid.ShouldBe(floorUid);
        floor.BuildingId.ShouldBe(10);
        floor.Rooms.ShouldBeNull();
    }

    [Fact]
    public async Task GetWorkspacesAsync_DelegatesToClient()
    {
        FakePythagorasClient client = new();
        client.SetGetAsyncResult(
            new Workspace { Id = 7, Name = "W" });

        PythagorasHandler service = new(client);

        IReadOnlyList<RoomModel> result = await service.GetRoomsAsync();

        client.GetAsyncCalled.ShouldBeTrue();
        client.LastEndpoint.ShouldBe("rest/v1/workspace/info");
        client.LastQuery.ShouldBeNull();
        RoomModel room = result.ShouldHaveSingleItem();
        room.Id.ShouldBe(7);
    }
}
