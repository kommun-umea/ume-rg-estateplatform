namespace Umea.se.Toolkit.Images.Caching;

/// <summary>
/// Builds human-readable blob paths for cached images.
/// </summary>
public static class BlobPathBuilder
{
    /// <summary>
    /// Builds a blob path for an image variant.
    /// Format: cache/{prefix}/{collection}/{id}/{variant}.{extension}
    /// Examples:
    ///   - cache/estateservice/images/12345/original.webp
    ///   - cache/estateservice/floors/123/blueprint.svg.gz
    /// </summary>
    public static string ForImage(string imageId, string variant)
    {
        string path = SanitizeForPath(imageId);
        string filename = variant == "svg" ? "blueprint.svg.gz" : $"{variant}.webp";
        return $"cache/{path}/{filename}";
    }

    private static string SanitizeForPath(string id)
        => id.Replace(":", "/").Replace("\\", "/");
}
