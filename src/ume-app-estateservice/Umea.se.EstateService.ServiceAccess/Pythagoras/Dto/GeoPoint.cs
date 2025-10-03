namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;

public sealed class GeoPoint : IPythagorasDto
{
    public double X { get; init; }
    public double Y { get; init; }
    public double Rotation { get; init; }
}
