using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Umea.se.EstateService.Shared.Exceptions;
using Umea.se.Toolkit.Images;

namespace Umea.se.EstateService.API.Infrastructure;

/// <summary>
/// Catches domain exceptions thrown from controller actions and converts them to Problem Details responses.
/// Registered before the toolkit's HttpResponseExceptionFilter so domain exceptions are handled here
/// rather than being caught by the toolkit's catch-all 500 handler.
/// </summary>
public sealed class EstateServiceExceptionFilter(ILogger<EstateServiceExceptionFilter> logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        (int statusCode, string title) = context.Exception switch
        {
            BusinessValidationException => (StatusCodes.Status400BadRequest, "Validation error"),
            EntityNotFoundException => (StatusCodes.Status404NotFound, "Not found"),
            StateConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            ImageNotFoundException => (StatusCodes.Status404NotFound, "Not found"),
            ImageTooLargeException => (StatusCodes.Status400BadRequest, "Invalid request"),
            ExternalServiceUnavailableException => (StatusCodes.Status502BadGateway, "Service unavailable"),
            _ => (0, string.Empty)
        };

        if (statusCode == 0)
        {
            return; // Not a domain exception — let the toolkit filter handle it
        }

        if (statusCode == StatusCodes.Status404NotFound)
        {
            // Log without the exception object so Application Insights doesn't track it as an Exception entry.
            // The HttpStatusSuccessProcessor already marks 404 responses as non-errors.
            logger.LogInformation("{Title}: {Message}", title, context.Exception.Message);
        }
        else
        {
            logger.LogWarning(context.Exception, "{Title}: {Message}", title, context.Exception.Message);
        }

        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = context.Exception.Message
        })
        {
            StatusCode = statusCode,
            ContentTypes = { "application/problem+json" }
        };

        context.ExceptionHandled = true;
    }
}
