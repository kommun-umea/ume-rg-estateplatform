namespace Umea.se.EstateService.Shared.Data.Entities;

public abstract class BaseEntity
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
