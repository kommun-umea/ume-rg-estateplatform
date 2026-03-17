using Umea.se.EstateService.Shared.Models;
using Umea.se.EstateService.Shared.ValueObjects;

namespace Umea.se.EstateService.Shared.Data.Entities;

public class BuildingEntity : NamedEntity
{
    public AddressModel? Address { get; set; }
    public GeoPointModel? GeoLocation { get; set; }
    public decimal GrossArea { get; set; }
    public decimal NetArea { get; set; }
    public string? YearOfConstruction { get; set; }
    public string? BuildingCondition { get; set; }
    public string? ExternalOwnerStatus { get; set; }
    public string? ExternalOwnerName { get; set; }
    public string? ExternalOwnerNote { get; set; }
    public string? PropertyDesignation { get; set; }
    public BuildingNoticeBoardModel? NoticeBoard { get; set; }
    public bool? BlueprintAvailable { get; set; }
    public BuildingContactPersonsModel? ContactPersons { get; set; }

    public BusinessTypeModel? BusinessType { get; set; }
    public int NumFloors { get; set; }
    public int NumRooms { get; set; }

    /// <summary>
    /// Gallery image IDs for this building. First image is the primary image.
    /// <para>
    /// Kept fresh at runtime by write-through from <see cref="Handlers.BuildingBackgroundCache"/>.
    /// <c>IReadOnlyList</c> ensures reference-swap atomicity on 64-bit CLR and
    /// prevents mutation while another thread reads.
    /// </para>
    /// </summary>
    public IReadOnlyList<int>? ImageIds { get; set; }

    /// <summary>
    /// Cached document count from Pythagoras. Null means not yet fetched.
    /// <para>
    /// Kept fresh at runtime by write-through from <see cref="Handlers.BuildingBackgroundCache"/>.
    /// </para>
    /// </summary>
    public int? NumDocuments { get; set; }

    /// <summary>
    /// When the background cache (document count + image IDs) was last fetched from Pythagoras (UTC).
    /// Shared timestamp — both properties are refreshed together.
    /// </summary>
    public DateTimeOffset? BackgroundCacheFetchedAtUtc { get; set; }

    public int EstateId { get; set; }

    public List<FloorEntity> Floors { get; set; } = [];
    public List<RoomEntity> Rooms { get; set; } = [];

    public override int? ParentId => EstateId;
}
