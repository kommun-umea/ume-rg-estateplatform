namespace Umea.se.EstateService.Shared.Exceptions;

/// <summary>
/// Fluent builder for constructing <see cref="BusinessValidationException"/> instances
/// with structured, per-field error codes that the frontend maps to localized messages.
/// </summary>
public sealed class ValidationErrorBuilder
{
    private readonly Dictionary<string, List<string>> _errors = [];

    public ValidationErrorBuilder AddError(string field, string errorCode)
    {
        if (!_errors.TryGetValue(field, out List<string>? list))
        {
            list = [];
            _errors[field] = list;
        }

        list.Add(errorCode);
        return this;
    }

    public bool HasErrors => _errors.Count > 0;

    public BusinessValidationException Build(string summary = "One or more validation errors occurred.")
        => new(summary, _errors.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray()));

    /// <summary>
    /// Throws a <see cref="BusinessValidationException"/> if any errors have been added.
    /// </summary>
    public void ThrowIfErrors(string summary = "One or more validation errors occurred.")
    {
        if (HasErrors)
        {
            throw Build(summary);
        }
    }
}
