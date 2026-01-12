namespace Umea.se.EstateService.Logic.Search;

public static class GeoHelper
{
    private const double EarthRadiusMeters = 6_371_000d;

    public static double CalculateDistanceInMeters(double latitude1, double longitude1, double latitude2, double longitude2)
    {
        if (double.IsNaN(latitude1) || double.IsNaN(longitude1) || double.IsNaN(latitude2) || double.IsNaN(longitude2))
        {
            return double.NaN;
        }

        double lat1Rad = DegreesToRadians(latitude1);
        double lat2Rad = DegreesToRadians(latitude2);
        double deltaLat = DegreesToRadians(latitude2 - latitude1);
        double deltaLon = DegreesToRadians(longitude2 - longitude1);

        double sinLat = Math.Sin(deltaLat / 2);
        double sinLon = Math.Sin(deltaLon / 2);
        double a = sinLat * sinLat + Math.Cos(lat1Rad) * Math.Cos(lat2Rad) * sinLon * sinLon;
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        double distance = EarthRadiusMeters * c;
        return double.IsFinite(distance) ? distance : double.NaN;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180d;
}
