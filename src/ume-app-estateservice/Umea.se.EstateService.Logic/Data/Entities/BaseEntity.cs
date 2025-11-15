namespace Umea.se.EstateService.Logic.Data.Entities;

public abstract class BaseEntity
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PopularName { get; set; } = string.Empty;
    public virtual int? ParentId { get; }
    public DateTimeOffset UpdatedAt { get; set; }
}
