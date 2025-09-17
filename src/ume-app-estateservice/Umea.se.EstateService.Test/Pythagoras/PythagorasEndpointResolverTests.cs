using Umea.se.EstateService.ServiceAccess.Pythagoras;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;

namespace Umea.se.EstateService.Test.Pythagoras;

public class PythagorasEndpointResolverTests
{
    [Fact]
    public void Resolve_UsesLowercaseDtoName_WhenNoAttributeIsPresent()
    {
        string result = PythagorasEndpointResolver.Resolve(typeof(Building));

        Assert.Equal("rest/v1/building", result);
    }

    private sealed class CustomDto
    {
    }

    [PythagorasEndpoint("custom-path")]
    private sealed class CustomMappedDto
    {
    }

    [Fact]
    public void Resolve_UsesAttributeValue_WhenAttributeIsPresent()
    {
        string result = PythagorasEndpointResolver.Resolve(typeof(CustomMappedDto));

        Assert.Equal("rest/v1/custom-path", result);
    }

    [Fact]
    public void Resolve_Throws_WhenAttributeIsEmpty()
    {
        Type dtoType = typeof(EmptyAttributeDto);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => PythagorasEndpointResolver.Resolve(dtoType));

        Assert.Contains("non-empty", exception.Message);
    }

    [PythagorasEndpoint(" ")]
    private sealed class EmptyAttributeDto
    {
    }
}
