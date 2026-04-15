using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Umea.se.EstateService.API.Infrastructure;
using Umea.se.EstateService.Shared.Exceptions;
using Umea.se.Toolkit.Images;

namespace Umea.se.EstateService.Test.Exceptions;

public class EstateServiceExceptionFilterTests
{
    private readonly EstateServiceExceptionFilter _filter = new(
        NullLogger<EstateServiceExceptionFilter>.Instance);

    [Fact]
    public void BusinessValidation_WithoutFieldErrors_ReturnsProblemDetails()
    {
        ExceptionContext context = CreateContext(new BusinessValidationException("Invalid input."));

        _filter.OnException(context);

        context.ExceptionHandled.ShouldBeTrue();
        ObjectResult result = context.Result.ShouldBeOfType<ObjectResult>();
        result.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);

        ProblemDetails problem = result.Value.ShouldBeOfType<ProblemDetails>();
        problem.Status.ShouldBe(400);
        problem.Title.ShouldBe("One or more validation errors occurred.");
        problem.Detail.ShouldBe("Invalid input.");
    }

    [Fact]
    public void BusinessValidation_WithFieldErrors_ReturnsValidationProblemDetails()
    {
        Dictionary<string, string[]> errors = new()
        {
            ["description"] = ["required"],
            ["files[0]"] = ["file_too_large"]
        };
        BusinessValidationException exception = new("Validation failed.", errors);

        ExceptionContext context = CreateContext(exception);

        _filter.OnException(context);

        context.ExceptionHandled.ShouldBeTrue();
        ObjectResult result = context.Result.ShouldBeOfType<ObjectResult>();
        result.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);

        ValidationProblemDetails problem = result.Value.ShouldBeOfType<ValidationProblemDetails>();
        problem.Status.ShouldBe(400);
        problem.Title.ShouldBe("One or more validation errors occurred.");
        problem.Detail.ShouldBe("Validation failed.");
        problem.Errors.ShouldContainKey("description");
        problem.Errors["description"].ShouldBe(["required"]);
        problem.Errors.ShouldContainKey("files[0]");
        problem.Errors["files[0]"].ShouldBe(["file_too_large"]);
    }

    [Fact]
    public void EntityNotFound_Returns404ProblemDetails()
    {
        ExceptionContext context = CreateContext(new EntityNotFoundException("Building 42 not found."));

        _filter.OnException(context);

        context.ExceptionHandled.ShouldBeTrue();
        ObjectResult result = context.Result.ShouldBeOfType<ObjectResult>();
        result.StatusCode.ShouldBe(StatusCodes.Status404NotFound);

        ProblemDetails problem = result.Value.ShouldBeOfType<ProblemDetails>();
        problem.Status.ShouldBe(404);
        problem.Title.ShouldBe("Not found");
    }

    [Fact]
    public void StateConflict_Returns409ProblemDetails()
    {
        ExceptionContext context = CreateContext(new StateConflictException("Already submitted."));

        _filter.OnException(context);

        context.ExceptionHandled.ShouldBeTrue();
        ObjectResult result = context.Result.ShouldBeOfType<ObjectResult>();
        result.StatusCode.ShouldBe(StatusCodes.Status409Conflict);

        ProblemDetails problem = result.Value.ShouldBeOfType<ProblemDetails>();
        problem.Status.ShouldBe(409);
        problem.Title.ShouldBe("Conflict");
    }

    [Fact]
    public void ExternalServiceUnavailable_Returns502ProblemDetails()
    {
        ExceptionContext context = CreateContext(new ExternalServiceUnavailableException("Pythagoras is down."));

        _filter.OnException(context);

        context.ExceptionHandled.ShouldBeTrue();
        ObjectResult result = context.Result.ShouldBeOfType<ObjectResult>();
        result.StatusCode.ShouldBe(StatusCodes.Status502BadGateway);

        ProblemDetails problem = result.Value.ShouldBeOfType<ProblemDetails>();
        problem.Status.ShouldBe(502);
        problem.Title.ShouldBe("Service unavailable");
    }

    [Fact]
    public void UnknownException_IsNotHandled()
    {
        ExceptionContext context = CreateContext(new InvalidOperationException("Something broke."));

        _filter.OnException(context);

        context.ExceptionHandled.ShouldBeFalse();
        context.Result.ShouldBeNull();
    }

    [Fact]
    public void ImageNotFoundException_Returns404()
    {
        ExceptionContext context = CreateContext(new ImageNotFoundException("img.png"));

        _filter.OnException(context);

        context.ExceptionHandled.ShouldBeTrue();
        ObjectResult result = context.Result.ShouldBeOfType<ObjectResult>();
        result.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void ImageTooLarge_Returns400()
    {
        ExceptionContext context = CreateContext(new ImageTooLargeException("Image exceeds maximum size."));

        _filter.OnException(context);

        context.ExceptionHandled.ShouldBeTrue();
        ObjectResult result = context.Result.ShouldBeOfType<ObjectResult>();
        result.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public void ResponseContentType_IsApplicationProblemJson()
    {
        ExceptionContext context = CreateContext(new EntityNotFoundException("not found"));

        _filter.OnException(context);

        ObjectResult result = context.Result.ShouldBeOfType<ObjectResult>();
        result.ContentTypes.ShouldContain("application/problem+json");
    }

    private static ExceptionContext CreateContext(Exception exception)
    {
        ActionContext actionContext = new(
            new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor());

        return new ExceptionContext(actionContext, [])
        {
            Exception = exception
        };
    }
}
