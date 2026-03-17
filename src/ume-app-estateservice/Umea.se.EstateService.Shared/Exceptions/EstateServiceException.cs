namespace Umea.se.EstateService.Shared.Exceptions;

public abstract class EstateServiceException(string message, Exception? innerException = null)
    : Exception(message, innerException);
