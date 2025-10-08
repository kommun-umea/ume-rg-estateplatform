using Umea.se.EstateService.Logic.Mappers;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enum;
using Umea.se.EstateService.Shared.Models;
using Umea.se.EstateService.Shared.ValueObjects;

namespace Umea.se.EstateService.Test.Pythagoras;

public class PythagorasBuildingInfoMapperTests
{
    [Fact]
    public void ToDomain_CopiesFields()
    {
        BuildingInfo dto = new()
        {
            Id = 5,
            Uid = Guid.NewGuid(),
            Name = "Info",
            PopularName = "Popular",
            MarkerType = PythMarkerType.Unknown,
            Grossarea = 12.3m,
            Netarea = 11.1m,
            SumGrossFloorarea = 13.4m,
            NumPlacedPersons = 7,
            GeoX = 63.1,
            GeoY = 20.2,
            GeoRotation = 10.5,
            AddressName = "Primary",
            AddressStreet = "Main Street",
            AddressZipCode = "12345",
            AddressCity = "Town",
            AddressCountry = "Country",
            AddressExtra = "Extra",
            Origin = "Source",
            CurrencyId = 17,
            CurrencyName = "SEK",
            FlagStatusIds = [1, 2, 3],
            BusinessTypeId = 6,
            BusinessTypeName = "Business",
            ProspectOfBuildingId = 9,
            IsProspect = true,
            ProspectStartDate = 1000,
            ExtraInfo = new() { ["key"] = "value" },
            PropertyValues = new() { ["prop"] = "123" },
            NavigationInfo = new() { ["nav"] = "info" }
        };

        BuildingInfoModel model = PythagorasBuildingInfoMapper.ToModel(dto);

        model.Id.ShouldBe(dto.Id);
        model.Uid.ShouldBe(dto.Uid);
        model.Name.ShouldBe("Info");
        model.PopularName.ShouldBe("Popular");
        model.GrossArea.ShouldBe(dto.Grossarea ?? 0);
        model.NetArea.ShouldBe(dto.Netarea ?? 0);
        model.SumGrossFloorArea.ShouldBe(dto.SumGrossFloorarea ?? 0);
        model.NumPlacedPersons.ShouldBe(dto.NumPlacedPersons);
        model.Address.ShouldBe(new AddressModel("Main Street", "12345", "Town", "Country", "Extra"));
        model.GeoLocation.ShouldNotBeNull();
        model.GeoLocation!.Lat.ShouldBe(dto.GeoX);
        model.GeoLocation.Lon.ShouldBe(dto.GeoY);
        model.ExtraInfo.ContainsKey("key").ShouldBeTrue();
        model.ExtraInfo["key"].ShouldBe("value");
        model.PropertyValues.ContainsKey("prop").ShouldBeTrue();
        model.PropertyValues["prop"].ShouldBe("123");
        model.NavigationInfo.ContainsKey("nav").ShouldBeTrue();
        model.NavigationInfo["nav"].ShouldBe("info");
    }

    [Fact]
    public void ToDomain_WithEmptyCollection_ReturnsEmptyArray()
    {
        IReadOnlyList<BuildingInfoModel> result = PythagorasBuildingInfoMapper.ToModel([]);
        result.ShouldBeSameAs(Array.Empty<BuildingInfoModel>());
    }
}
