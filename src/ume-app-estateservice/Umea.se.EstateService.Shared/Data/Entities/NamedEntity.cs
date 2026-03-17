namespace Umea.se.EstateService.Shared.Data.Entities;

public abstract class NamedEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string PopularName { get; set; } = string.Empty;
    public virtual int? ParentId { get; }
}
