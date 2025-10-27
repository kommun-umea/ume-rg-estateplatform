using Shouldly;
using Umea.se.EstateService.Logic.Mappers;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Test.Pythagoras;

public class PythagorasEstateMapperTests
{
    [Fact]
    public void ToModel_WhenIncludingBuildings_CopiesCountAndCollection()
    {
        NavigationFolder dto = new()
        {
            Id = 1,
            Buildings = new[]
            {
                new Building { Id = 10 },
                new Building { Id = 11 }
            }
        };

        EstateModel model = PythagorasEstateMapper.ToModel(dto, includeBuildings: true);

        model.BuildingCount.ShouldBe(2);
        model.Buildings.ShouldNotBeNull();
        model.Buildings!.Length.ShouldBe(2);
    }

    [Fact]
    public void ToModel_WhenExcludingBuildings_KeepsCount()
    {
        NavigationFolder dto = new()
        {
            Id = 1,
            Buildings = new[]
            {
                new Building { Id = 10 },
                new Building { Id = 11 },
                new Building { Id = 12 }
            }
        };

        EstateModel model = PythagorasEstateMapper.ToModel(dto, includeBuildings: false);

        model.BuildingCount.ShouldBe(3);
        model.Buildings.ShouldBeNull();
    }
}
