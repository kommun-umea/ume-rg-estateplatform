namespace Umea.se.EstateService.Shared.Exceptions;

public sealed class BusinessValidationException : EstateServiceException
{
    public IDictionary<string, string[]> Errors { get; }

    public BusinessValidationException(string message)
        : base(message)
    {
        Errors = new Dictionary<string, string[]>();
    }

    public BusinessValidationException(string message, IDictionary<string, string[]> errors)
        : base(message)
    {
        Errors = errors;
    }
}
