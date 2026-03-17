namespace Umea.se.EstateService.API.Responses;

public sealed class DocumentRecordTypesResponse
{
    public required IReadOnlyList<DocumentRecordTypeItem> ActionTypes { get; init; }
}

public sealed class DocumentRecordTypeItem
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public required IReadOnlyList<DocumentRecordTypeStatusItem> Statuses { get; init; }
}

public sealed class DocumentRecordTypeStatusItem
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool ReceivedDateIsRelevant { get; init; }
}
