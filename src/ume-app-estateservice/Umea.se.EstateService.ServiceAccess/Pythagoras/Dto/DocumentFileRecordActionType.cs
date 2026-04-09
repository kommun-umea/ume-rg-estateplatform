namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;

public sealed class DocumentFileRecordActionType : IPythagorasDto
{
    public int Id { get; init; }
    public Guid Uid { get; init; }
    public string Name { get; init; } = string.Empty;
    public int? NumberOfRecordYears { get; init; }
    public int? NumberOfRecordMonths { get; init; }
    public bool? OverrideIsRecorded { get; init; }
    public bool? OverrideIsGDPR { get; init; }
    public bool? IsRecordedReadonly { get; init; }
    public bool? IsSecuredReadonly { get; init; }
    public bool? IsGDPRReadonly { get; init; }
    public string? ForceRemovalProcess { get; init; }
}

public sealed class DocumentFileRecordActionTypeStatus : IPythagorasDto
{
    public int Id { get; init; }
    public Guid Uid { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool ReceivedDateIsRelevant { get; init; }
}
