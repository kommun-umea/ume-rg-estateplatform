namespace Umea.se.EstateService.Shared.Search;

public sealed record GeoCoordinate(double Latitude, double Longitude);

public sealed record GeoFilter(GeoCoordinate Coordinate, int RadiusMeters);
