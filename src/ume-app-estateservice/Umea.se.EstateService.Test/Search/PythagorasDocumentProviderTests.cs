using Umea.se.EstateService.Logic.Providers;
using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Models;
using Umea.se.EstateService.Shared.Search;
using Umea.se.EstateService.Shared.ValueObjects;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enum;

namespace Umea.se.EstateService.Test.Search;

public class PythagorasDocumentProviderTests
{
    [Fact]
    public async Task GetDocumentsAsync_PopulatesBuildingAndRoomAddresses()
    {
        FakePythagorasHandler handler = new();
        PythagorasDocumentProvider provider = new(handler);

        ICollection<PythagorasDocument> documents = await provider.GetDocumentsAsync();

        documents.ShouldNotBeEmpty();

        PythagorasDocument estate = documents.Single(d => d.Type == NodeType.Estate && d.Id == FakePythagorasHandler.EstateId);
        estate.NumChildren.ShouldBe(1);
        estate.GrossArea.ShouldBe(123.45m);

        PythagorasDocument building = documents.Single(d => d.Type == NodeType.Building && d.Id == FakePythagorasHandler.BuildingId);
        building.Address.ShouldBe("Skolgatan 31A 901 84 Umeå");
        building.NumChildren.ShouldBe(1);
        building.GrossArea.ShouldBe(555m);

        PythagorasDocument room = documents.Single(d => d.Type == NodeType.Room && d.Id == FakePythagorasHandler.RoomId);
        room.Address.ShouldBe(building.Address);
        room.NumChildren.ShouldBe(0);
        room.GrossArea.ShouldBe(42.25m);
    }

    private sealed class FakePythagorasHandler : IPythagorasHandler
    {
        internal const int BuildingId = 42;
        internal const int EstateId = 7;
        internal const int RoomId = 99;

        public Task<IReadOnlyList<EstateModel>> GetEstatesAsync(PythagorasQuery<NavigationFolder>? query = null, CancellationToken cancellationToken = default)
        {
            EstateModel estate = new()
            {
                Id = EstateId,
                Name = "Estate",
                PopularName = "Estate Popular",
                GrossArea = 123.45m,
                Buildings =
                [
                    new BuildingModel
                    {
                        Id = BuildingId,
                        Name = "Building",
                        PopularName = "Popular Building"
                    }
                ]
            };

            return Task.FromResult<IReadOnlyList<EstateModel>>([estate]);
        }

        public Task<IReadOnlyList<BuildingInfoModel>> GetBuildingsAsync(PythagorasQuery<BuildingInfo>? query = null, CancellationToken cancellationToken = default)
        {
            AddressModel address = new("Skolgatan 31A", "901 84", "Umeå", "Sverige", string.Empty);
            BuildingInfoModel model = new()
            {
                Id = BuildingId,
                Name = "Building Info",
                Address = address,
                GrossArea = 555m
            };

            return Task.FromResult<IReadOnlyList<BuildingInfoModel>>([model]);
        }

        public Task<IReadOnlyList<RoomModel>> GetRoomsAsync(PythagorasQuery<Workspace>? query = null, CancellationToken cancellationToken = default)
        {
            RoomModel room = new()
            {
                Id = RoomId,
                Name = "Room",
                BuildingId = BuildingId,
                GrossArea = 42.25
            };

            return Task.FromResult<IReadOnlyList<RoomModel>>([room]);
        }

        public Task<IReadOnlyList<BuildingInfoModel>> GetBuildingInfoAsync(PythagorasQuery<BuildingInfo>? query = null, int? navigationFolderId = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<BuildingRoomModel>> GetBuildingWorkspacesAsync(int buildingId, PythagorasQuery<BuildingWorkspace>? query = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<FloorInfoModel>> GetBuildingFloorsAsync(int buildingId, PythagorasQuery<Floor>? floorQuery = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<FloorInfoModel>> GetBuildingFloorsWithRoomsAsync(int buildingId, PythagorasQuery<Floor>? floorQuery = null, PythagorasQuery<BuildingWorkspace>? workspaceQuery = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<BuildingAscendantModel>> GetBuildingAscendantsAsync(int buildingId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<BuildingAscendantModel>>([]);

        public Task<IReadOnlyList<FloorInfoModel>> GetFloorsAsync(PythagorasQuery<Floor>? query = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<BuildingInfoModel?> GetBuildingByIdAsync(int buildingId, BuildingIncludeOptions includeOptions = BuildingIncludeOptions.None, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
