using Umea.se.EstateService.ServiceAccess.Pythagoras;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Pythagoras;
using DomainMarkerType = Umea.se.EstateService.Shared.Pythagoras.MarkerType;
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

        BuildingModel model = PythagorasBuildingMapper.ToDomain(dto);

        Assert.Equal(dto.Id, model.Id);
        Assert.Equal(dto.Uid, model.Uid);
        Assert.Equal(dto.Version, model.Version);
        Assert.Equal(dto.Created, model.Created);
        Assert.Equal(dto.Updated, model.Updated);
        Assert.Equal(dto.Name, model.Name);
        Assert.Equal(dto.PopularName, model.PopularName);
        Assert.Equal((DomainMarkerType)dto.MarkerType, model.MarkerType);
        Assert.NotNull(model.GeoLocation);
        Assert.Equal(dto.GeoLocation.X, model.GeoLocation.X);
        Assert.Equal(dto.GeoLocation.Y, model.GeoLocation.Y);
        Assert.Equal(dto.GeoLocation.Rotation, model.GeoLocation.Rotation);
        Assert.Equal(dto.Origin, model.Origin);
        Assert.Equal(dto.PropertyTax, model.PropertyTax);
        Assert.True(model.UseWeightsInWorkspaceAreaDistribution);
    }

    [Fact]
    public void ToDomain_WithEmptyCollection_ReturnsEmptyArray()
    {
        IReadOnlyList<BuildingModel> result = PythagorasBuildingMapper.ToDomain([]);

        Assert.Same(Array.Empty<BuildingModel>(), result);
    }

    [Fact]
    public void ToDomain_AllowsNullGeoPoint()
    {
        Building dto = new()
        {
            GeoLocation = null!
        };

        BuildingModel model = PythagorasBuildingMapper.ToDomain(dto);

        Assert.NotNull(model.GeoLocation);
        Assert.Equal(0, model.GeoLocation.X);
    }
}
