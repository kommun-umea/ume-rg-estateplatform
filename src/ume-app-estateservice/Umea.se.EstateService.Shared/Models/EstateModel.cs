using System.Text.Json.Serialization;
using Umea.se.EstateService.Shared.Data;
using Umea.se.EstateService.Shared.Search;
using Umea.se.EstateService.Shared.ValueObjects;

namespace Umea.se.EstateService.Shared.Models;

public sealed class EstateModel : ISearchable, IFavoriteable
{
    [JsonIgnore]
    public NodeType FavoriteNodeType => NodeType.Estate;
    public int Id { get; init; }
    public Guid Uid { get; init; }
    public string Name { get; init; } = string.Empty;
    public string PopularName { get; init; } = string.Empty;
    public decimal GrossArea { get; init; }
    public decimal NetArea { get; init; }
    public GeoPointModel? GeoLocation { get; init; }
    public AddressModel? Address { get; init; }
    public BuildingModel[]? Buildings { get; set; }
    public int BuildingCount { get; init; }
    public EstateExtendedPropertiesModel? ExtendedProperties { get; init; }
    public bool? IsFavorite { get; set; }
    public DateTimeOffset UpdatedAt => DateTime.Now;
}
