using System.Text.Json.Serialization;

namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;

public sealed class GalleryImageFile : IPythagorasDto
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public int Version { get; set; }

    [JsonConverter(typeof(UnixMillisDateTimeConverter))]
    public DateTime Created { get; set; }

    [JsonConverter(typeof(UnixMillisDateTimeConverter))]
    public DateTime Updated { get; set; }

    public string? Name { get; set; }
    public long DataSize { get; set; }
    public string? Description { get; set; }
    public double? Width { get; set; }
    public double? Height { get; set; }
}
