namespace Umea.se.EstateService.Logic.Exceptions;

public sealed class FloorBlueprintValidationException(string message)
    : Exception(message);
