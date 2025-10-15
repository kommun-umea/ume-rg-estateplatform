namespace Umea.se.EstateService.Logic.Exceptions;

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
