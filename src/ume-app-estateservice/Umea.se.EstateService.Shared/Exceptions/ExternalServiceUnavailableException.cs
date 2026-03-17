namespace Umea.se.EstateService.Shared.Exceptions;

public sealed class ExternalServiceUnavailableException(string message, Exception? innerException = null)
    : EstateServiceException(message, innerException);
