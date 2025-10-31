using Shouldly;
using Umea.se.EstateService.Logic.Search;

namespace Umea.se.EstateService.Test.Search;

public class GeoHelperTests
{
    [Fact]
    public void CalculateDistanceInMeters_ReturnsZeroForSamePoint()
    {
        double distance = GeoHelper.CalculateDistanceInMeters(63.8258, 20.2630, 63.8258, 20.2630);

        distance.ShouldBe(0d, 0.001d);
    }

    [Fact]
    public void CalculateDistanceInMeters_ReturnsExpectedDistanceBetweenCities()
    {
        // Approximate distance between Umeå and Stockholm
        double distance = GeoHelper.CalculateDistanceInMeters(63.8258, 20.2630, 59.3293, 18.0686);

        distance.ShouldBe(512_000d, 5_000d);
    }
}
