using Umea.se.EstateService.Logic.Mappers;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Models;
using Umea.se.EstateService.Shared.ValueObjects;

namespace Umea.se.EstateService.Test.Pythagoras;

public class PythagorasBuildingAscendantMapperTests
{
    [Fact]
    public void ToModel_FiltersNonSpaceManagerOrigins()
    {
        BuildingAscendant[] input =
        [
            new() { Id = 1, Name = "SpaceManager", Origin = "SpaceManager" },
            new() { Id = 2, Name = "Other", Origin = "OtherSystem" }
        ];

        IReadOnlyList<BuildingAscendantModel> result = PythagorasBuildingAscendantMapper.ToModel(input);

        result.ShouldHaveSingleItem().Id.ShouldBe(1);
    }

    [Fact]
    public void ToModel_AssignsTypesAndGeoLocation()
    {
        BuildingAscendant[] input =
        [
            new()
            {
                Id = 1,
                Name = "Estate",
                Origin = "SpaceManager",
                GeoLocation = new GeoPoint { X = 12.3, Y = 45.6, Rotation = 0 }
            },
            new()
            {
                Id = 2,
                Name = "Area",
                Origin = "SpaceManager",
                GeoLocation = new GeoPoint { X = 0, Y = 0, Rotation = 0 }
            },
            new()
            {
                Id = 3,
                Name = "Org",
                Origin = "SpaceManager"
            }
        ];

        IReadOnlyList<BuildingAscendantModel> result = PythagorasBuildingAscendantMapper.ToModel(input);

        result.Count.ShouldBe(3);

        result[0].Type.ShouldBe(BuildingAscendantType.Estate);
        result[0].GeoLocation.ShouldBe(new GeoPointModel(12.3, 45.6));

        result[1].Type.ShouldBe(BuildingAscendantType.Area);
        result[1].GeoLocation.ShouldBeNull();

        result[2].Type.ShouldBe(BuildingAscendantType.Organization);
    }
}
