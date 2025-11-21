namespace Umea.se.EstateService.Shared.Search;

public sealed record GeoCoordinate(double Latitude, double Longitude);

public abstract record GeoFilter
{
    private protected GeoFilter()
    {
    }
}

public sealed record GeoRadiusFilter(GeoCoordinate Coordinate, int RadiusMeters) : GeoFilter;

public sealed record GeoBoundingBoxFilter(GeoCoordinate SouthWest, GeoCoordinate NorthEast) : GeoFilter;
