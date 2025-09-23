using Umea.se.EstateService.Logic.Mappers;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Models;
using DomainMarkerType = Umea.se.EstateService.Shared.Models.MarkerType;
using TransportMarkerType = Umea.se.EstateService.ServiceAccess.Pythagoras.Dto.MarkerType;

namespace Umea.se.EstateService.Test.Pythagoras;

public class PythagorasBuildingMapperTests
{
    [Fact]
    public void ToDomain_CopiesFields()
    {
        Building dto = new()
        {
            Id = 7,
            Uid = Guid.NewGuid(),
            Version = 2,
            Created = 123,
            Updated = 456,
            Name = "Main",
            PopularName = "HQ",
            MarkerType = TransportMarkerType.Unknown,
            GeoLocation = new GeoPoint { X = 1.1, Y = 2.2, Rotation = 3.3 },
            Origin = "manual",
            PropertyTax = 42.5m,
            UseWeightsInWorkspaceAreaDistribution = true
        };

        BuildingModel model = PythagorasBuildingMapper.ToModel(dto);

        model.Id.ShouldBe(dto.Id);
        model.Uid.ShouldBe(dto.Uid);
        model.Version.ShouldBe(dto.Version);
        model.Created.ShouldBe(dto.Created);
        model.Updated.ShouldBe(dto.Updated);
        model.Name.ShouldBe(dto.Name);
        model.PopularName.ShouldBe(dto.PopularName);
        model.MarkerType.ShouldBe((DomainMarkerType)dto.MarkerType);

        GeoPointModel? location = model.GeoLocation;
        location.ShouldNotBeNull();
        location!.X.ShouldBe(dto.GeoLocation.X);
        location.Y.ShouldBe(dto.GeoLocation.Y);
        location.Rotation.ShouldBe(dto.GeoLocation.Rotation);

        model.Origin.ShouldBe(dto.Origin);
        model.PropertyTax.ShouldBe(dto.PropertyTax);
        model.UseWeightsInWorkspaceAreaDistribution.ShouldBeTrue();
    }

    [Fact]
    public void ToDomain_WithEmptyCollection_ReturnsEmptyArray()
    {
        IReadOnlyList<BuildingModel> result = PythagorasBuildingMapper.ToModel([]);
        result.ShouldBeSameAs(Array.Empty<BuildingModel>());
    }

    [Fact]
    public void ToDomain_AllowsNullGeoPoint()
    {
        Building dto = new()
        {
            GeoLocation = null!
        };

        BuildingModel model = PythagorasBuildingMapper.ToModel(dto);

        model.GeoLocation.ShouldNotBeNull();
        model.GeoLocation!.X.ShouldBe(0);
    }
}
