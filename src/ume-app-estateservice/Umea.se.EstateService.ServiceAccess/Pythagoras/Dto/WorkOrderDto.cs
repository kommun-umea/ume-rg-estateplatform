using System.Text.Json;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enums;

namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;

public sealed class WorkOrderDto : IPythagorasDto
{
    public int Id { get; init; }
    public Guid Uid { get; init; }
    public int Version { get; init; }
    public long Created { get; init; }
    public long Updated { get; init; }
    public string? Name { get; init; }
    public long? RegistrationDate { get; init; }
    public long? LatestExecutionDate { get; init; }
    public long? LatestExecutionDateFromPriority { get; init; }
    public bool Vandalism { get; init; }
    public bool IsWarranty { get; init; }
    public long? FailureDate { get; init; }
    public string? Origin { get; init; }
    public string? CreatedByUsername { get; init; }
    public string? NotifierUsername { get; init; }
    public string? NotifierName { get; init; }
    public string? NotifierTelephone { get; init; }
    public string? NotifierEmail { get; init; }
    public bool ShowInPythagorasHome { get; init; }
    public decimal? MaterialMarkupPercent { get; init; }
    public string? BoundObjectType { get; init; }
    public string? OurReference { get; init; }
    public bool ToBeInvoiced { get; init; }
    public JsonElement? InvoicingDate { get; init; }
    public string? InvoicingDescription { get; init; }
    public string? InvoicingYourReference { get; init; }
    public string? LastUpdatedFirstname { get; init; }
    public string? LastUpdatedLastname { get; init; }
    public string? LastUpdatedUsername { get; init; }
    public int? StatusId { get; init; }
    public string? StatusName { get; init; }
}

public sealed class CreatePythagorasWorkOrderRequest
{
    public string? Description { get; init; }
    public WorkOrderBoundObjectType? BoundObjectType { get; init; }
    public List<int>? BoundObjectIds { get; init; }
    public string? NotifierUsername { get; init; }
    public string? NotifierName { get; init; }
    public string? NotifierTelephone { get; init; }
    public string? NotifierEmail { get; init; }
    public int? StatusId { get; init; }
    public int? PriorityId { get; init; }
    public int? CategoryId { get; init; }
    public int? OperatingGroupId { get; init; }
    public List<int?>? AssigneeIds { get; init; }
    public Dictionary<string, object>? CustomFieldsValueMap { get; init; }
    public List<WorkOrderInlineDocument>? Documents { get; init; }
}

/// <summary>
/// Extended work order info from POST /rest/v1/workorder/info.
/// Includes statusCategory which is not available on the basic WorkOrderDto.
/// </summary>
public sealed class WorkOrderInfoDto
{
    public int Id { get; init; }
    public string? Name { get; init; }
    public int? StatusId { get; init; }
    public string? StatusName { get; init; }
    public string? StatusCategory { get; init; }
    public string? NotifierUsername { get; init; }
    public string? NotifierName { get; init; }
    public string? NotifierEmail { get; init; }
}

public sealed class WorkOrderInlineDocument
{
    public string FileName { get; init; } = null!;
    public long FileSize { get; init; }
    public string File { get; init; } = null!;
}
