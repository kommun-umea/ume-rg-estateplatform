using Umea.se.EstateService.Shared.Data.Entities;

namespace Umea.se.EstateService.DataStore.Entities;

/// <summary>
/// EF Core entity storing the pre-computed ascendant hierarchy for a building.
/// Contains Estate, Region, and Organization ascendants.
/// </summary>
public class BuildingAscendantDbEntity
{
    /// <summary>
    /// Primary key - matches the BuildingId.
    /// </summary>
    public int BuildingId { get; set; }

    // Estate ascendant (nullable)
    public int? EstateAscendantId { get; set; }
    public string? EstateAscendantName { get; set; }
    public string? EstateAscendantPopularName { get; set; }
    public double? EstateAscendantGeoLat { get; set; }
    public double? EstateAscendantGeoLon { get; set; }

    // Region ascendant (nullable)
    public int? RegionAscendantId { get; set; }
    public string? RegionAscendantName { get; set; }
    public string? RegionAscendantPopularName { get; set; }
    public double? RegionAscendantGeoLat { get; set; }
    public double? RegionAscendantGeoLon { get; set; }

    // Organization ascendant (nullable)
    public int? OrganizationAscendantId { get; set; }
    public string? OrganizationAscendantName { get; set; }
    public string? OrganizationAscendantPopularName { get; set; }
    public double? OrganizationAscendantGeoLat { get; set; }
    public double? OrganizationAscendantGeoLon { get; set; }

    // Navigation property (ignored in EF configuration but kept for potential use)
    public BuildingEntity? Building { get; set; }
}
