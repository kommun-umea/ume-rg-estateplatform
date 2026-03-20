using Umea.se.EstateService.Logic.Handlers;
using Umea.se.EstateService.Shared.Data.Entities;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Test.Handlers;

public class EstateModelMapperTests
{
    private static BuildingEntity CreateBuilding(BuildingNoticeBoardModel? noticeBoard = null) =>
        new()
        {
            Id = 1,
            Name = "B1",
            PopularName = "B1",
            NoticeBoard = noticeBoard
        };

    [Fact]
    public void MapBuildingInfo_NoNoticeBoard_ReturnsNull()
    {
        BuildingInfoModel result = EstateModelMapper.MapBuildingInfo(CreateBuilding());

        result.ExtendedProperties!.NoticeBoard.ShouldBeNull();
    }

    [Fact]
    public void MapBuildingInfo_NoDates_ReturnsNoticeBoard()
    {
        BuildingEntity building = CreateBuilding(new BuildingNoticeBoardModel { Text = "Hello" });

        BuildingInfoModel result = EstateModelMapper.MapBuildingInfo(building);

        result.ExtendedProperties!.NoticeBoard.ShouldNotBeNull();
        result.ExtendedProperties!.NoticeBoard!.Text.ShouldBe("Hello");
    }

    [Fact]
    public void MapBuildingInfo_ExpiredEndDate_ReturnsNull()
    {
        BuildingEntity building = CreateBuilding(new BuildingNoticeBoardModel
        {
            Text = "Expired",
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 12, 31)
        });

        BuildingInfoModel result = EstateModelMapper.MapBuildingInfo(building);

        result.ExtendedProperties!.NoticeBoard.ShouldBeNull();
    }

    [Fact]
    public void MapBuildingInfo_FutureStartDate_ReturnsNull()
    {
        BuildingEntity building = CreateBuilding(new BuildingNoticeBoardModel
        {
            Text = "Future",
            StartDate = DateTime.Today.AddDays(30),
            EndDate = DateTime.Today.AddDays(60)
        });

        BuildingInfoModel result = EstateModelMapper.MapBuildingInfo(building);

        result.ExtendedProperties!.NoticeBoard.ShouldBeNull();
    }

    [Fact]
    public void MapBuildingInfo_CurrentlyActive_ReturnsNoticeBoard()
    {
        BuildingEntity building = CreateBuilding(new BuildingNoticeBoardModel
        {
            Text = "Active",
            StartDate = DateTime.Today.AddDays(-10),
            EndDate = DateTime.Today.AddDays(10)
        });

        BuildingInfoModel result = EstateModelMapper.MapBuildingInfo(building);

        result.ExtendedProperties!.NoticeBoard.ShouldNotBeNull();
        result.ExtendedProperties!.NoticeBoard!.Text.ShouldBe("Active");
    }

    [Fact]
    public void MapBuildingInfo_EndDateToday_ReturnsNoticeBoard()
    {
        BuildingEntity building = CreateBuilding(new BuildingNoticeBoardModel
        {
            Text = "Last day",
            EndDate = DateTime.Today
        });

        BuildingInfoModel result = EstateModelMapper.MapBuildingInfo(building);

        result.ExtendedProperties!.NoticeBoard.ShouldNotBeNull();
    }

    [Fact]
    public void MapBuildingInfo_StartDateToday_ReturnsNoticeBoard()
    {
        BuildingEntity building = CreateBuilding(new BuildingNoticeBoardModel
        {
            Text = "First day",
            StartDate = DateTime.Today
        });

        BuildingInfoModel result = EstateModelMapper.MapBuildingInfo(building);

        result.ExtendedProperties!.NoticeBoard.ShouldNotBeNull();
    }
}
