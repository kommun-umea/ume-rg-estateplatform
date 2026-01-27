namespace Umea.se.Toolkit.Images;

public record ImageResult(byte[] Data, string ContentType, bool IsGzipped)
{
    public static ImageResult WebP(byte[] data) => new(data, "image/webp", false);
    public static ImageResult SvgGzipped(byte[] data) => new(data, "image/svg+xml", true);
}
