using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Umea.se.EstateService.API.Controllers.Requests;

/// <summary>
/// Provides shared paging and search query parameters for list endpoints.
/// </summary>
public record PagedQueryRequest : IValidatableObject
{
    /// <summary>
    /// The default number of items returned when no limit is provided.
    /// </summary>
    public const int DefaultLimit = 50;

    /// <summary>
    /// The maximum number of items that can be requested in a single response.
    /// </summary>
    public const int MaxLimit = 200;

    /// <summary>
    /// The maximum length allowed for search terms.
    /// </summary>
    public const int MaxSearchTermLength = 200;

    /// <summary>
    /// Maximum number of records to return.
    /// </summary>
    [FromQuery(Name = "limit")]
    [Range(-1, MaxLimit, ErrorMessage = "Limit must be -1 (no limit) or between 1 and {2}.")]
    [SwaggerParameter("Maximum number of items to return. Use -1 to disable the limit.", Required = false)]
    public int Limit { get; init; } = DefaultLimit;

    /// <summary>
    /// Number of records to skip before returning results.
    /// </summary>
    [FromQuery(Name = "offset")]
    [Range(0, int.MaxValue, ErrorMessage = "Offset must be zero or greater.")]
    [SwaggerParameter("Number of items to skip from the start of the result set.", Required = false)]
    public int Offset { get; init; }

    /// <summary>
    /// Search expression applied to the resource when supported.
    /// </summary>
    [FromQuery(Name = "searchTerm")]
    [StringLength(MaxSearchTermLength, ErrorMessage = "SearchTerm cannot exceed {1} characters.")]
    [SwaggerParameter("Optional search term used to filter results.", Required = false)]
    public string? SearchTerm { get; init; }

    /// <inheritdoc />
    public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Limit == 0 || Limit < -1)
        {
            yield return new ValidationResult(
                $"Limit must be -1 (no limit) or between 1 and {MaxLimit}.",
                new[] { nameof(Limit) });
        }
    }
}
