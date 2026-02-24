namespace Umea.se.EstateService.Logic.Handlers.Blueprint;

public sealed class FloorBlueprintValidationException(string message)
    : Exception(message);
