namespace Umea.se.EstateService.Shared.ValueObjects;

public sealed record GeoPointModel
{
    public double Lat { get; init; }
    public double Lon { get; init; }

    public GeoPointModel() { }

    public GeoPointModel(double lat, double lon)
    {
        Lat = lat;
        Lon = lon;
    }
}

