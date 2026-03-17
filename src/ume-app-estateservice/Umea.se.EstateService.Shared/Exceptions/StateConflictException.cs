namespace Umea.se.EstateService.Shared.Exceptions;

public sealed class StateConflictException(string message)
    : EstateServiceException(message);
