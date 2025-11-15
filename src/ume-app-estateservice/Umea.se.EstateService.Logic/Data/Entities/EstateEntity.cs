using Umea.se.EstateService.Shared.ValueObjects;

namespace Umea.se.EstateService.Logic.Data.Entities;

public class EstateEntity : BaseEntity
{
    public decimal GrossArea { get; set; }
    public decimal NetArea { get; set; }
    public GeoPointModel? GeoLocation { get; set; }
    public AddressModel? Address { get; set; }
    public string? PropertyDesignation { get; set; }
    public string? OperationalArea { get; set; }
    public string? MunicipalityArea { get; set; }
    public int BuildingCount { get; set; }

    public List<BuildingEntity> Buildings { get; set; } = [];
}
