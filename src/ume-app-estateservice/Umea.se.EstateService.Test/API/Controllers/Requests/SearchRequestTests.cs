using System.ComponentModel.DataAnnotations;
using Umea.se.EstateService.API.Controllers.Requests;

namespace Umea.se.EstateService.Test.API.Controllers.Requests;

public class SearchRequestTests
{
    [Fact]
    public void Validate_AllowsGeoOnlySearch()
    {
        SearchRequest request = new()
        {
            Query = string.Empty,
            Latitude = 63.8258,
            Longitude = 20.2630,
            RadiusMeters = 1_000
        };

        IList<ValidationResult> results = Validate(request);

        results.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_ReturnsError_ForInvalidLatitude()
    {
        SearchRequest request = new()
        {
            Query = string.Empty,
            Latitude = 120,
            Longitude = 10,
            RadiusMeters = 1_000
        };

        IList<ValidationResult> results = Validate(request);

        results.ShouldContain(result => result.MemberNames.Contains(nameof(SearchRequest.Latitude)));
    }

    [Fact]
    public void Validate_ReturnsError_WhenGeoParametersIncomplete()
    {
        SearchRequest request = new()
        {
            Query = string.Empty,
            Latitude = 63.8258
        };

        IList<ValidationResult> results = Validate(request);

        results.ShouldContain(result => result.MemberNames.Contains(nameof(SearchRequest.RadiusMeters)));
    }

    private static IList<ValidationResult> Validate(SearchRequest request)
    {
        ValidationContext context = new(request);
        List<ValidationResult> results = [];
        Validator.TryValidateObject(request, context, results, validateAllProperties: true);
        return results;
    }
}
