namespace Umea.se.EstateService.API.Responses;

public sealed class WorkOrderConfigResponse
{
    public int MaxFileCount { get; init; }
    public long MaxFileSizeBytes { get; init; }
    public List<string> AllowedContentTypes { get; init; } = [];
}
