namespace Umea.se.EstateService.Shared.Models;

public sealed class BuildingExtendedPropertiesModel
{
    public string? ExternalOwner { get; init; }
    public string? PropertyDesignation { get; init; }
    public string? OperationsGroupValue { get; init; }
    public BuildingNoticeBoardModel? NoticeBoard { get; init; }
}

public sealed class BuildingNoticeBoardModel
{
    public string? Text { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
}
