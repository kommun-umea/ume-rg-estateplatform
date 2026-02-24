namespace Umea.se.EstateService.Logic.Handlers.Blueprint;

public sealed class FloorBlueprintUnavailableException : Exception
{
    public FloorBlueprintUnavailableException(string message)
        : base(message)
    {
    }

    public FloorBlueprintUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
