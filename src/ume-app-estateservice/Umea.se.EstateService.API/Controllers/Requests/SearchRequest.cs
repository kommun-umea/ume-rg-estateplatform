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

    [FromQuery(Name = "south")]
    public double? SouthLatitude { get; init; }

    [FromQuery(Name = "west")]
    public double? WestLongitude { get; init; }

    [FromQuery(Name = "north")]
    public double? NorthLatitude { get; init; }

    [FromQuery(Name = "east")]
    public double? EastLongitude { get; init; }

    [JsonIgnore]
    internal GeoFilter? GeoFilter
    {
        get
        {
            if (Latitude is double lat && Longitude is double lon && RadiusMeters is int radius)
            {
                return new GeoRadiusFilter(new GeoCoordinate(lat, lon), radius);
            }

            if (SouthLatitude is double south &&
                WestLongitude is double west &&
                NorthLatitude is double north &&
                EastLongitude is double east)
            {
                return new GeoBoundingBoxFilter(
                    new GeoCoordinate(south, west),
                    new GeoCoordinate(north, east));
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
        bool hasSouth = SouthLatitude.HasValue;
        bool hasWest = WestLongitude.HasValue;
        bool hasNorth = NorthLatitude.HasValue;
        bool hasEast = EastLongitude.HasValue;
        bool hasGeoBox = hasSouth || hasWest || hasNorth || hasEast;

        if (hasGeo && hasGeoBox)
        {
            yield return new ValidationResult(
                "Provide either a radius-based geo filter or a bounding box, not both.",
                [nameof(Latitude), nameof(Longitude), nameof(RadiusMeters), nameof(SouthLatitude), nameof(WestLongitude), nameof(NorthLatitude), nameof(EastLongitude)]);
        }

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

        if (hasGeoBox)
        {
            if (!hasSouth || !hasWest || !hasNorth || !hasEast)
            {
                yield return new ValidationResult(
                    "South, west, north, and east must be provided together.",
                    [nameof(SouthLatitude), nameof(WestLongitude), nameof(NorthLatitude), nameof(EastLongitude)]);
            }
            else
            {
                double south = SouthLatitude!.Value;
                double north = NorthLatitude!.Value;
                double west = WestLongitude!.Value;
                double east = EastLongitude!.Value;

                if (double.IsNaN(south) || south < -90 || south > 90)
                {
                    yield return new ValidationResult(
                        "South latitude must be between -90 and 90 degrees.",
                        [nameof(SouthLatitude)]);
                }

                if (double.IsNaN(north) || north < -90 || north > 90)
                {
                    yield return new ValidationResult(
                        "North latitude must be between -90 and 90 degrees.",
                        [nameof(NorthLatitude)]);
                }

                if (double.IsNaN(west) || west < -180 || west > 180)
                {
                    yield return new ValidationResult(
                        "West longitude must be between -180 and 180 degrees.",
                        [nameof(WestLongitude)]);
                }

                if (double.IsNaN(east) || east < -180 || east > 180)
                {
                    yield return new ValidationResult(
                        "East longitude must be between -180 and 180 degrees.",
                        [nameof(EastLongitude)]);
                }

                if (south >= north)
                {
                    yield return new ValidationResult(
                        "South latitude must be less than north latitude.",
                        [nameof(SouthLatitude), nameof(NorthLatitude)]);
                }

                if (west >= east)
                {
                    yield return new ValidationResult(
                        "West longitude must be less than east longitude.",
                        [nameof(WestLongitude), nameof(EastLongitude)]);
                }
            }
        }

        if (hasQuery && trimmedQuery.Length < MinQueryLength)
        {
            yield return new ValidationResult(
                $"Query must be at least {MinQueryLength} characters when provided.",
                [nameof(Query)]);
        }
        else if (!hasQuery && !hasGeo && !hasGeoBox)
        {
            yield return new ValidationResult(
                "Query must be provided unless geospatial parameters are specified.",
                [nameof(Query)]);
        }
    }
}
