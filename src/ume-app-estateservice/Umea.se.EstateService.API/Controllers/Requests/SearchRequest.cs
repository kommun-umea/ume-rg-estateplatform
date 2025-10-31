using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Umea.se.EstateService.Shared.Autocomplete;
using Umea.se.EstateService.Shared.Search;

namespace Umea.se.EstateService.API.Controllers.Requests;

public sealed record SearchRequest : IValidatableObject
{
    public const int MinQueryLength = 2;
    public const int MaxLimit = 1000;
    public const int MaxRadiusMeters = 50_000;

    [FromQuery(Name = "type")]
    public HashSet<AutocompleteType> Type { get; init; } = [AutocompleteType.Any];

    [FromQuery(Name = "query")]
    public string? Query { get; init; } = string.Empty;

    [FromQuery(Name = "limit")]
    [Range(1, MaxLimit, ErrorMessage = "Limit must be between {1} and {2}.")]
    public int Limit { get; init; } = 10;

    [FromQuery(Name = "lat")]
    public double? Latitude { get; init; }

    [FromQuery(Name = "lon")]
    public double? Longitude { get; init; }

    [FromQuery(Name = "radius")]
    public int? RadiusMeters { get; init; }

    [JsonIgnore]
    public GeoFilter? GeoFilter
    {
        get
        {
            if (Latitude is double lat && Longitude is double lon && RadiusMeters is int radius)
            {
                return new GeoFilter(new GeoCoordinate(lat, lon), radius);
            }

            return null;
        }
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Type.Count > 1 && Type.Contains(AutocompleteType.Any))
        {
            yield return new ValidationResult(
                "The 'Any' type cannot be combined with other values.",
                [nameof(Type)]);
        }

        bool hasQuery = !string.IsNullOrWhiteSpace(Query);
        string trimmedQuery = Query?.Trim() ?? string.Empty;
        bool hasLatitude = Latitude.HasValue;
        bool hasLongitude = Longitude.HasValue;
        bool hasRadius = RadiusMeters.HasValue;
        bool hasGeo = hasLatitude || hasLongitude || hasRadius;

        if (hasGeo)
        {
            if (!hasLatitude || !hasLongitude || !hasRadius)
            {
                yield return new ValidationResult(
                    "Latitude, longitude, and radius must be provided together.",
                    [nameof(Latitude), nameof(Longitude), nameof(RadiusMeters)]);
            }
            else
            {
                double latitude = Latitude!.Value;
                if (double.IsNaN(latitude) || latitude < -90 || latitude > 90)
                {
                    yield return new ValidationResult(
                        "Latitude must be between -90 and 90 degrees.",
                        [nameof(Latitude)]);
                }

                double longitude = Longitude!.Value;
                if (double.IsNaN(longitude) || longitude < -180 || longitude > 180)
                {
                    yield return new ValidationResult(
                        "Longitude must be between -180 and 180 degrees.",
                        [nameof(Longitude)]);
                }

                int radius = RadiusMeters!.Value;
                if (radius <= 0 || radius > MaxRadiusMeters)
                {
                    yield return new ValidationResult(
                        $"Radius must be between 1 and {MaxRadiusMeters} meters.",
                        [nameof(RadiusMeters)]);
                }
            }
        }

        if (hasQuery && trimmedQuery.Length < MinQueryLength)
        {
            yield return new ValidationResult(
                $"Query must be at least {MinQueryLength} characters when provided.",
                [nameof(Query)]);
        }
        else if (!hasQuery && !hasGeo)
        {
            yield return new ValidationResult(
                "Query must be provided unless geospatial parameters are specified.",
                [nameof(Query)]);
        }
    }
}
