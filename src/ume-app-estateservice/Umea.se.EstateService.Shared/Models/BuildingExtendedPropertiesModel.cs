namespace Umea.se.EstateService.Shared.Models;

public sealed class BuildingExtendedPropertiesModel
{
    public bool? BlueprintAvailable { get; init; }
    public string? PropertyDesignation { get; init; }
    public string? OperationsGroupValue { get; init; }
    public BuildingNoticeBoardModel? NoticeBoard { get; init; }
    public BuildingContactPersonsModel? ContactPersons { get; init; }
    public ExternalOwnerInfoModel? ExternalOwnerInfo { get; init; }
    public string? YearOfConstruction { get; set; }
}

public sealed class BuildingNoticeBoardModel
{
    public string? Text { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
}
