using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Annotations;
using Umea.se.EstateService.Shared.Exceptions;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.API.Requests;

public class CreateWorkOrderFormRequest
{
    [Required(ErrorMessage = ValidationErrorCode.Required)]
    [JsonPropertyName("buildingId")]
    [SwaggerSchema("Pythagoras building ID.")]
    public int BuildingId { get; init; }

    [Required(ErrorMessage = ValidationErrorCode.Required)]
    [JsonPropertyName("workOrderType")]
    [SwaggerSchema("Type of work order.")]
    public WorkOrderType WorkOrderType { get; init; }

    [Required(ErrorMessage = ValidationErrorCode.Required)]
    [JsonPropertyName("location")]
    [SwaggerSchema("Where the issue is located.", Description = "indoor | outdoor")]
    public string Location { get; init; } = null!;

    [JsonPropertyName("roomId")]
    [SwaggerSchema("Room ID. Required for indoor, must be omitted for outdoor.", Nullable = true)]
    public int? RoomId { get; init; } = null;

    [Required(ErrorMessage = ValidationErrorCode.Required)]
    [StringLength(2000, ErrorMessage = ValidationErrorCode.MaxLength)]
    [JsonPropertyName("description")]
    [SwaggerSchema("Description of the issue.")]
    public string Description { get; init; } = "";

    [JsonPropertyName("notifierEmail")]
    [SwaggerSchema("Override notifier email. Defaults to authenticated user's email.", Nullable = true)]
    [EmailAddress(ErrorMessage = ValidationErrorCode.InvalidFormat)]
    [StringLength(200, ErrorMessage = ValidationErrorCode.MaxLength)]
    public string? NotifierEmail { get; init; }

    [JsonPropertyName("notifierName")]
    [SwaggerSchema("Override notifier name. Defaults to null.", Nullable = true)]
    [StringLength(200, ErrorMessage = ValidationErrorCode.MaxLength)]
    public string? NotifierName { get; init; }

    [JsonPropertyName("notifierPhone")]
    [SwaggerSchema("Notifier phone number.", Nullable = true)]
    [RegularExpression(@"^[\d\s+\-()]+$", ErrorMessage = ValidationErrorCode.InvalidFormat)]
    [StringLength(50, ErrorMessage = ValidationErrorCode.MaxLength)]
    public string? NotifierPhone { get; init; }

    [JsonPropertyName("files")]
    [SwaggerSchema("Attached files (images, documents).")]
    public List<IFormFile>? Files { get; init; }
}
