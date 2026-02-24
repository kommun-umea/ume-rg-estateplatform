using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Shared.Data;

/// <summary>
/// Holds the three-level ascendant hierarchy for a single building.
/// Pre-computed during data refresh and stored in IDataStore for fast lookup.
/// </summary>
public sealed class BuildingAscendantTriplet
{
    /// <summary>
    /// The estate (property/real estate) that this building belongs to.
    /// Typically the immediate parent in the navigation tree.
    /// </summary>
    public BuildingAscendantModel? Estate { get; init; }

    /// <summary>
    /// The geographic region/area/district that contains the estate.
    /// Maps to NavigationFolderType: District, SubMunicipality, or Municipality.
    /// </summary>
    public BuildingAscendantModel? Region { get; init; }

    /// <summary>
    /// The organizational unit that manages the estate.
    /// Maps to NavigationFolderType: ManagementObject or similar.
    /// </summary>
    public BuildingAscendantModel? Organization { get; init; }
}
