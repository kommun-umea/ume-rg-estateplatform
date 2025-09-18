using Umea.se.EstateService.ServiceAccess.Pythagoras;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Pythagoras;

namespace Umea.se.EstateService.Test.Pythagoras;

public class PythagorasWorkspaceMapperTests
{
    [Fact]
    public void ToDomain_BuildingWorkspace_CopiesCoreFields()
    {
        BuildingWorkspace dto = new()
        {
            Id = 1,
            Uid = Guid.NewGuid(),
            Created = 100,
            Updated = 200,
            Name = "W-1",
            PopularName = "Workspace",
            GrossArea = 10,
            NetArea = 9,
            UpliftedArea = 1,
            CommonArea = 0.5,
            Cost = 1000,
            Price = 1200,
            Capacity = 2,
            OptimalCapacity = 3,
            FloorId = 5,
            FloorUid = Guid.NewGuid(),
            FloorName = "Floor",
            FloorPopularName = "Popular",
            BuildingId = 7,
            BuildingUid = Guid.NewGuid(),
            BuildingName = "Building",
            BuildingPopularName = "B",
            BuildingOrigin = "Origin",
            StatusName = "Status",
            StatusColor = "#fff",
            RentalStatusName = "Rental",
            RentalStatusColor = "#000"
        };

        BuildingWorkspaceModel model = PythagorasWorkspaceMapper.ToDomain(dto);

        Assert.Equal(dto.Id, model.Id);
        Assert.Equal(dto.Uid, model.Uid);
        Assert.Equal(dto.Name, model.Name);
        Assert.Equal(dto.PopularName, model.PopularName);
        Assert.Equal(dto.GrossArea, model.GrossArea);
        Assert.Equal(dto.StatusName, model.StatusName);
        Assert.Equal(dto.RentalStatusColor, model.RentalStatusColor);
        Assert.Equal(dto.BuildingName, model.BuildingName);
    }

    [Fact]
    public void ToDomain_Workspace_CopiesCoreFields()
    {
        Workspace dto = new()
        {
            Id = 2,
            Uid = Guid.NewGuid(),
            Version = 3,
            Created = 50,
            Updated = 60,
            Name = "Desk",
            PopularName = "My Desk",
            GrossArea = 4.5,
            NetArea = 4.3,
            UpliftedArea = 0.2,
            CommonArea = 0.1,
            Cost = 500,
            Price = 700,
            Capacity = 1,
            OptimalCapacity = 2
        };

        WorkspaceModel model = PythagorasWorkspaceMapper.ToDomain(dto);

        Assert.Equal(dto.Id, model.Id);
        Assert.Equal(dto.Version, model.Version);
        Assert.Equal(dto.PopularName, model.PopularName);
        Assert.Equal(dto.Price, model.Price);
        Assert.Equal(dto.OptimalCapacity, model.OptimalCapacity);
    }
}
