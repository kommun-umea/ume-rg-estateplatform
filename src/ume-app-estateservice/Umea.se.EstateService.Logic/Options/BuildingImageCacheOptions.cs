namespace Umea.se.EstateService.Logic.Options;

public sealed class BuildingImageCacheOptions
{
    public const string SectionName = "BuildingImageCache";

    /// <summary>
    /// Number of hours to cache building image metadata. Defaults to 24 hours.
    /// </summary>
    public int MetadataExpirationHours { get; init; } = 24;
}
