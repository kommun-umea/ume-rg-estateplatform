using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Umea.se.EstateService.Shared.Autocomplete;

namespace Umea.se.EstateService.API.Controllers.Requests;

public sealed record AutocompleteRequest : IValidatableObject
{
    public const int MinQueryLength = 2;
    public const int MaxLimit = 1000;

    [FromQuery(Name = "type")]
    public HashSet<AutocompleteType> Type { get; init; } = [AutocompleteType.Any];

    [FromQuery(Name = "query")]
    [Required]
    [MinLength(MinQueryLength, ErrorMessage = "Query must be at least {1} characters.")]
    public string Query { get; init; } = string.Empty;

    [FromQuery(Name = "limit")]
    [Range(1, MaxLimit, ErrorMessage = "Limit must be between {1} and {2}.")]
    public int Limit { get; init; } = 10;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Type.Count > 1 && Type.Contains(AutocompleteType.Any))
        {
            yield return new ValidationResult(
                "The 'Any' type cannot be combined with other values.",
                [nameof(Type)]);
        }
    }
}
