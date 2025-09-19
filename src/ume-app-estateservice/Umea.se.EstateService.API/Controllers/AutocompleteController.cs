using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Umea.se.EstateService.Logic.Handlers;
using Umea.se.EstateService.Shared.Autocomplete;
using Umea.se.Toolkit.Auth;

namespace Umea.se.EstateService.API.Controllers;

[ApiController]
[Produces("application/json")]
[Route(ApiRoutes.Autocomplete)]
[AuthorizeApiKey]
public class AutocompleteController(IAutocompleteHandler autocompleteHandler, ILogger<AutocompleteController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AutocompleteResult>> GetAsync([FromQuery] AutocompleteRequest request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Autocomplete request for {Type} (Limit={Limit})", request.Type, request.Limit);

        AutocompleteArgs args = new() { Query = request.Query, Limit = request.Limit, Type = request.Type, BuildingId = request.BuildingId };
        AutocompleteResult response = await autocompleteHandler.SearchAsync(args, cancellationToken);
        return Ok(response);
    }
}

public sealed record AutocompleteRequest : IValidatableObject
{
    public const int MinQueryLength = 2;
    public const int MaxLimit = 1000;

    [FromQuery(Name = "type")]
    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AutocompleteType Type { get; init; } = AutocompleteType.Any;

    [FromQuery(Name = "query")]
    [Required]
    [MinLength(MinQueryLength, ErrorMessage = "Query must be at least {1} characters.")]
    public string Query { get; init; } = string.Empty;

    [FromQuery(Name = "limit")]
    [Range(1, MaxLimit, ErrorMessage = "Limit must be between {1} and {2}.")]
    public int Limit { get; init; } = 10;

    [FromQuery(Name = "buildingId")]
    [Range(1, int.MaxValue, ErrorMessage = "BuildingId must be positive when provided.")]
    public int? BuildingId { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Type is AutocompleteType.Workspace && BuildingId is int id && id <= 0)
        {
            yield return new ValidationResult(
                "BuildingId must be positive when provided for workspace autocomplete.",
                [nameof(BuildingId)]);
        }
    }
}

