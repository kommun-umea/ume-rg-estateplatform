namespace Umea.se.EstateService.Shared.Exceptions;

public sealed class BusinessValidationException(string message)
    : EstateServiceException(message);
