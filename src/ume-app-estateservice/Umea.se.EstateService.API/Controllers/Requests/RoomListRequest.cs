using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Umea.se.EstateService.API.Controllers.Requests;

/// <summary>
/// Query parameters for retrieving rooms.
/// </summary>
public sealed record RoomListRequest : PagedQueryRequest
{
    /// <summary>
    /// Explicit room identifiers to include in the response.
    /// </summary>
    [FromQuery(Name = "ids")]
    [MaxLength(MaxIds, ErrorMessage = "You may specify at most {1} ids.")]
    [SwaggerParameter("Optional list of room IDs to filter by.", Required = false)]
    public int[]? Ids { get; init; }

    /// <summary>
    /// Optional identifier of the building to scope the results.
    /// </summary>
    [FromQuery(Name = "buildingId")]
    [Range(1, int.MaxValue, ErrorMessage = "BuildingId must be positive when provided.")]
    [SwaggerParameter("Optional building identifier to scope rooms.", Required = false)]
    public int? BuildingId { get; init; }

    /// <summary>
    /// Gets the maximum allowed count for <see cref="Ids"/>.
    /// </summary>
    public const int MaxIds = 100;

    /// <summary>
    /// Runs custom validation logic for combination rules.
    /// </summary>
    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (ValidationResult validationResult in base.Validate(validationContext))
        {
            yield return validationResult;
        }

        if (Ids is { Length: > 0 })
        {
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                yield return new ValidationResult(
                    "Specify either ids or searchTerm, not both.",
                    [nameof(Ids), nameof(SearchTerm)]);
            }

            if (BuildingId is not null)
            {
                yield return new ValidationResult(
                    "Ids cannot be combined with buildingId filtering.",
                    [nameof(Ids), nameof(BuildingId)]);
            }
        }
    }

    internal ImmutableArray<int> GetIdsOrEmpty()
    {
        if (Ids is not { Length: > 0 })
        {
            return [];
        }

        return [.. Ids];
    }
}
