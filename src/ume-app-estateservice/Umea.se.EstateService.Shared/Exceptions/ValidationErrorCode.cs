namespace Umea.se.EstateService.Shared.Exceptions;

/// <summary>
/// Machine-readable error codes returned in <see cref="BusinessValidationException.Errors"/>.
/// The frontend maps these to localized user-facing messages.
/// </summary>
public static class ValidationErrorCode
{
    public const string Required = "required";
    public const string InvalidValue = "invalid_value";
    public const string InvalidFormat = "invalid_format";
    public const string MaxLength = "max_length";
    public const string NotFound = "not_found";
    public const string FileTooLarge = "file_too_large";
    public const string TooManyFiles = "too_many_files";
    public const string InvalidContentType = "invalid_content_type";
    public const string UnrecognizedFileContent = "unrecognized_file_content";
    public const string ContentTypeMismatch = "content_type_mismatch";
    public const string NotSupported = "not_supported";
    public const string Conflict = "conflict";
}
