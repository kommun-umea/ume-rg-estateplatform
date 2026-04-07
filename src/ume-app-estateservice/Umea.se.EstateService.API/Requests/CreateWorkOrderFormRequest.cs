using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Annotations;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.API.Requests;

public class CreateWorkOrderFormRequest
{
    [Required]
    [JsonPropertyName("buildingId")]
    [SwaggerSchema("Pythagoras building ID.")]
    public int BuildingId { get; init; }

    [Required]
    [JsonPropertyName("workOrderType")]
    [SwaggerSchema("Type of work order.")]
    public WorkOrderType WorkOrderType { get; init; }

    [Required]
    [JsonPropertyName("location")]
    [SwaggerSchema("Where the issue is located.", Description = "indoor | outdoor")]
    [RegularExpression("^(indoor|outdoor)$", ErrorMessage = "Location must be 'indoor' or 'outdoor'.")]
    public string Location { get; init; } = null!;

    [JsonPropertyName("roomId")]
    [SwaggerSchema("Room ID. Required for indoor, must be omitted for outdoor.", Nullable = true)]
    public int? RoomId { get; init; } = null;

    [Required]
    [JsonPropertyName("description")]
    [SwaggerSchema("Description of the issue.")]
    public string Description { get; init; } = "";

    [JsonPropertyName("notifierEmail")]
    [SwaggerSchema("Override notifier email. Defaults to authenticated user's email.", Nullable = true)]
    [EmailAddress]
    public string? NotifierEmail { get; init; }

    [JsonPropertyName("notifierName")]
    [SwaggerSchema("Override notifier name. Defaults to null.", Nullable = true)]
    public string? NotifierName { get; init; }

    [JsonPropertyName("files")]
    [SwaggerSchema("Attached files (images, documents).")]
    public List<IFormFile>? Files { get; init; }
}
