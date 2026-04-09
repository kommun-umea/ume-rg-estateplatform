namespace Umea.se.EstateService.DataStore.Entities;

public class BuildingDocumentEntity
{
    public int BuildingId { get; set; }
    public int DocumentId { get; set; }
    public required string Name { get; set; }
    public long? SizeInBytes { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public DateTimeOffset FetchedAtUtc { get; set; }
}
