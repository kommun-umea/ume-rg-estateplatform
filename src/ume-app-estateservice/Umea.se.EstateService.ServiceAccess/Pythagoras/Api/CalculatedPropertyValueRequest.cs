namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Api;

/// <summary>
/// Represents optional query parameters for the calculated property value endpoint.
/// </summary>
public sealed class CalculatedPropertyValueRequest
{
    /// <summary>
    /// Gets or sets the property ids to calculate. When null or empty, all properties are calculated.
    /// </summary>
    public IReadOnlyCollection<int>? PropertyIds { get; init; }

    /// <summary>
    /// Gets or sets the navigation id to scope calculations to.
    /// </summary>
    public int? NavigationId { get; init; }

    /// <summary>
    /// Gets or sets the preferred currency.
    /// </summary>
    public string? UserCurrency { get; init; }

    /// <summary>
    /// Gets or sets the preferred length unit.
    /// </summary>
    public string? UserLengthUnit { get; init; }

    /// <summary>
    /// Gets or sets the preferred weight unit.
    /// </summary>
    public string? UserWeightUnit { get; init; }

    /// <summary>
    /// Gets or sets the evaluation timestamp as an epoch based integer.
    /// </summary>
    public long? When { get; init; }

    internal string BuildQueryString()
    {
        List<string> parameters = [];

        if (PropertyIds is { Count: > 0 })
        {
            foreach (int propertyId in PropertyIds)
            {
                parameters.Add($"propertyIds[]={propertyId}");
            }
        }

        if (NavigationId is int navigationId)
        {
            parameters.Add($"navigationId={navigationId}");
        }

        AppendIfNotEmpty(parameters, "userCurrency", UserCurrency);
        AppendIfNotEmpty(parameters, "userLengthUnit", UserLengthUnit);
        AppendIfNotEmpty(parameters, "userWeightUnit", UserWeightUnit);

        if (When is long whenValue)
        {
            parameters.Add($"when={whenValue}");
        }

        return parameters.Count == 0 ? string.Empty : string.Join('&', parameters);
    }

    private static void AppendIfNotEmpty(List<string> parameters, string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        parameters.Add($"{name}={Uri.EscapeDataString(value)}");
    }
}
