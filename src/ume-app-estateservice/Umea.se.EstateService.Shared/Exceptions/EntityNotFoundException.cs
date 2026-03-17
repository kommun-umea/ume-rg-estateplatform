namespace Umea.se.EstateService.Shared.Exceptions;

public sealed class EntityNotFoundException(string message)
    : EstateServiceException(message);
