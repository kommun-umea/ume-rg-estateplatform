using Umea.se.EstateService.Logic.Handlers.WorkOrder;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enums;
using Umea.se.EstateService.Shared.Infrastructure.ConfigurationModels;

namespace Umea.se.EstateService.Test.Handlers;

/// <summary>
/// PythagorasWorkOrderCreateRequirements is the single source of truth for which per-type
/// fields Pythagoras requires on create and therefore which config keys must be present at
/// startup. Lock in its contract here so silent drift (e.g. someone adding a new submissible
/// type to the requirements map but not to config) is caught.
/// </summary>
public class PythagorasWorkOrderCreateRequirementsTests
{
    [Theory]
    [InlineData(PythagorasWorkOrderType.BuildingService, true)]
    [InlineData(PythagorasWorkOrderType.ErrorReport, false)]
    [InlineData(PythagorasWorkOrderType.FacilityService, false)]
    [InlineData(PythagorasWorkOrderType.TownHallService, false)]
    [InlineData(PythagorasWorkOrderType.SpaceRequirement, false)] // not submissible from this app
    public void RequiresCategory_ReturnsExpected(PythagorasWorkOrderType type, bool expected)
        => PythagorasWorkOrderCreateRequirements.RequiresCategory(type).ShouldBe(expected);

    [Theory]
    [InlineData(PythagorasWorkOrderType.ErrorReport, true)]
    [InlineData(PythagorasWorkOrderType.FacilityService, true)]
    [InlineData(PythagorasWorkOrderType.TownHallService, true)]
    [InlineData(PythagorasWorkOrderType.BuildingService, false)]
    [InlineData(PythagorasWorkOrderType.SpaceRequirement, false)]
    public void RequiresOperatingGroup_ReturnsExpected(PythagorasWorkOrderType type, bool expected)
        => PythagorasWorkOrderCreateRequirements.RequiresOperatingGroup(type).ShouldBe(expected);

    [Fact]
    public void Validate_FullConfig_ReturnsNoProblems()
    {
        WorkOrderConfiguration config = BuildValidConfig();

        PythagorasWorkOrderCreateRequirements.Validate(config).ShouldBeEmpty();
    }

    [Fact]
    public void Validate_MissingCategoryDefault_ReportsKey()
    {
        WorkOrderConfiguration config = BuildValidConfig();
        config.DefaultCategoryIdByType.Remove((int)PythagorasWorkOrderType.BuildingService);

        IReadOnlyList<string> problems = PythagorasWorkOrderCreateRequirements.Validate(config);

        problems.ShouldHaveSingleItem().ShouldContain("DefaultCategoryIdByType:2");
    }

    [Fact]
    public void Validate_MissingOperatingGroupDefault_ReportsKey()
    {
        WorkOrderConfiguration config = BuildValidConfig();
        config.DefaultOperatingGroupIdByType.Remove((int)PythagorasWorkOrderType.ErrorReport);

        IReadOnlyList<string> problems = PythagorasWorkOrderCreateRequirements.Validate(config);

        problems.ShouldHaveSingleItem().ShouldContain("DefaultOperatingGroupIdByType:1");
    }

    [Fact]
    public void Validate_MissingMultipleDefaults_ReportsAll()
    {
        WorkOrderConfiguration config = BuildValidConfig();
        config.DefaultCategoryIdByType.Clear();
        config.DefaultOperatingGroupIdByType.Clear();

        IReadOnlyList<string> problems = PythagorasWorkOrderCreateRequirements.Validate(config);

        // 1 category-required type + 3 operating-group-required types = 4 problems
        problems.Count.ShouldBe(4);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    [InlineData(-1.0)]
    [InlineData(2.0)]
    [InlineData(double.NaN)]
    public void Validate_ThresholdOutOfRange_Reports(double threshold)
    {
        WorkOrderConfiguration config = BuildValidConfig();
        config.CategoryClassifierMinimumConfidence = threshold;

        IReadOnlyList<string> problems = PythagorasWorkOrderCreateRequirements.Validate(config);

        problems.ShouldHaveSingleItem().ShouldContain("CategoryClassifierMinimumConfidence");
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(0.75)]
    [InlineData(1.0)]
    public void Validate_ThresholdInRange_NoProblem(double threshold)
    {
        WorkOrderConfiguration config = BuildValidConfig();
        config.CategoryClassifierMinimumConfidence = threshold;

        PythagorasWorkOrderCreateRequirements.Validate(config).ShouldBeEmpty();
    }

    private static WorkOrderConfiguration BuildValidConfig() => new()
    {
        CategoryClassifierMinimumConfidence = 0.75,
        DefaultCategoryIdByType = new Dictionary<int, int>
        {
            [(int)PythagorasWorkOrderType.BuildingService] = 82,
        },
        DefaultOperatingGroupIdByType = new Dictionary<int, int>
        {
            [(int)PythagorasWorkOrderType.ErrorReport] = 16,
            [(int)PythagorasWorkOrderType.FacilityService] = 21,
            [(int)PythagorasWorkOrderType.TownHallService] = 22,
        },
    };
}
