namespace Umea.se.EstateService.Shared.Models;

/// <summary>
/// Holds calculated property values that extend the estate domain model.
/// </summary>
public sealed class EstateExtendedPropertiesModel
{
    public string? OperationalArea { get; init; }
    public string? MunicipalityArea { get; init; }
    public string? PropertyDesignation { get; init; }
}
