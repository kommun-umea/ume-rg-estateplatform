using Umea.se.EstateService.Shared.Exceptions;

namespace Umea.se.EstateService.Test.Exceptions;

public class BusinessValidationExceptionTests
{
    [Fact]
    public void Constructor_MessageOnly_HasEmptyErrors()
    {
        BusinessValidationException exception = new("Something went wrong.");

        exception.Message.ShouldBe("Something went wrong.");
        exception.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Constructor_WithErrors_PreservesErrorsDictionary()
    {
        Dictionary<string, string[]> errors = new()
        {
            ["field1"] = ["error1", "error2"],
            ["field2"] = ["error3"]
        };

        BusinessValidationException exception = new("Validation failed.", errors);

        exception.Message.ShouldBe("Validation failed.");
        exception.Errors.Count.ShouldBe(2);
        exception.Errors["field1"].ShouldBe(["error1", "error2"]);
        exception.Errors["field2"].ShouldBe(["error3"]);
    }

    [Fact]
    public void IsEstateServiceException()
    {
        BusinessValidationException exception = new("test");

        exception.ShouldBeAssignableTo<EstateServiceException>();
    }
}
