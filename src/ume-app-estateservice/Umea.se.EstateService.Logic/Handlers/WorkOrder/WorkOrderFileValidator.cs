using Umea.se.EstateService.Shared.Exceptions;
using Umea.se.EstateService.Shared.Infrastructure;
using Umea.se.EstateService.Shared.Infrastructure.ConfigurationModels;

namespace Umea.se.EstateService.Logic.Handlers.WorkOrder;

public class WorkOrderFileValidator(ApplicationConfig appConfig) : IWorkOrderFileValidator
{
    private readonly WorkOrderFileValidationConfig _config = appConfig.WorkOrderProcessing.FileValidation;

    // Magic byte signatures for common file types.
    // Order matters: longer/more-specific signatures should come first for a given MIME type.
    private static readonly (string MimeType, byte[] Magic)[] _magicSignatures =
    [
        ("application/pdf",  [0x25, 0x50, 0x44, 0x46]),                         // %PDF
        ("image/png",        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]), // .PNG....
        ("image/jpeg",       [0xFF, 0xD8, 0xFF]),                               // JFIF/EXIF/generic JPEG
        ("image/gif",        [0x47, 0x49, 0x46, 0x38]),                         // GIF8 (covers GIF87a and GIF89a)
        ("image/webp",       [0x52, 0x49, 0x46, 0x46]),                         // RIFF (WebP container)
        ("image/bmp",        [0x42, 0x4D]),                                     // BM
        ("image/tiff",       [0x49, 0x49, 0x2A, 0x00]),                         // II*. (little-endian TIFF)
        ("image/tiff",       [0x4D, 0x4D, 0x00, 0x2A]),                         // MM.* (big-endian TIFF)
        ("image/heic",       []),                                               // checked via ftyp box below
    ];

    // ftyp-based formats (HEIC/HEIF) — the "ftyp" marker sits at offset 4
    private static readonly byte[] _ftypMarker = [0x66, 0x74, 0x79, 0x70];       // "ftyp"
    private static readonly string[] _heicBrands = ["heic", "heix", "mif1"];

    // Maximum bytes we need to read for any signature check
    private const int MaxHeaderBytes = 12;

    public async Task ValidateAsync(IReadOnlyList<WorkOrderFileUpload> files, CancellationToken cancellationToken = default)
    {
        if (files.Count > _config.MaxFileCount)
        {
            throw new BusinessValidationException($"Too many files. Maximum is {_config.MaxFileCount}, got {files.Count}.");
        }

        for (int i = 0; i < files.Count; i++)
        {
            WorkOrderFileUpload file = files[i];

            if (file.FileSize > _config.MaxFileSizeBytes)
            {
                throw new BusinessValidationException(
                    $"File '{file.FileName}' is too large ({file.FileSize} bytes). Maximum is {_config.MaxFileSizeBytes} bytes.");
            }

            if (!IsAllowedContentType(file.ContentType))
            {
                throw new BusinessValidationException(
                    $"File '{file.FileName}' has disallowed content type '{file.ContentType}'. Allowed types: {string.Join(", ", _config.AllowedContentTypes)}.");
            }

            string? detectedType = await DetectContentTypeAsync(file.Stream, cancellationToken) ?? throw new BusinessValidationException(
                    $"File '{file.FileName}' could not be identified. The file content does not match any known file format.");

            if (!ContentTypeMatchesClaimed(detectedType, file.ContentType))
            {
                throw new BusinessValidationException(
                    $"File '{file.FileName}' content does not match its declared type '{file.ContentType}'. Detected: '{detectedType}'.");
            }
        }
    }

    private static async Task<string?> DetectContentTypeAsync(Stream stream, CancellationToken ct)
    {
        byte[] header = new byte[MaxHeaderBytes];
        long originalPosition = stream.CanSeek ? stream.Position : -1;

        int bytesRead = await ReadExactAsync(stream, header, ct);

        if (stream.CanSeek)
        {
            stream.Position = originalPosition;
        }

        if (bytesRead < 2)
        {
            return null;
        }

        // Check ftyp-based formats (HEIC/HEIF) — "ftyp" at offset 4, brand at offset 8
        if (bytesRead >= 12 && header.AsSpan(4, 4).SequenceEqual(_ftypMarker))
        {
            string brand = System.Text.Encoding.ASCII.GetString(header, 8, 4).ToLowerInvariant();
            if (Array.Exists(_heicBrands, b => b == brand))
            {
                return "image/heic";
            }
        }

        // Check fixed-offset magic signatures
        foreach ((string mimeType, byte[] magic) in _magicSignatures)
        {
            if (magic.Length == 0)
            {
                continue; // handled above (ftyp-based)
            }

            if (bytesRead >= magic.Length && header.AsSpan(0, magic.Length).SequenceEqual(magic))
            {
                // RIFF container could be WebP or something else — verify "WEBP" at offset 8
                if (mimeType == "image/webp")
                {
                    if (bytesRead >= 12 && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50)
                    {
                        return "image/webp";
                    }

                    continue; // RIFF but not WebP
                }

                return mimeType;
            }
        }

        return null;
    }

    private static async Task<int> ReadExactAsync(Stream stream, byte[] buffer, CancellationToken ct)
    {
        int totalRead = 0;
        while (totalRead < buffer.Length)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(totalRead, buffer.Length - totalRead), ct);
            if (read == 0)
            {
                break;
            }

            totalRead += read;
        }

        return totalRead;
    }

    /// <summary>
    /// Checks whether the detected MIME type is compatible with the claimed type.
    /// For example, detected "image/jpeg" is compatible with claimed "image/jpeg",
    /// and both are compatible with a wildcard allow rule like "image/*".
    /// </summary>
    private static bool ContentTypeMatchesClaimed(string detected, string claimed)
    {
        if (string.Equals(detected, claimed, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Allow same family — e.g. detected "image/heic" is fine if claimed is "image/heif"
        string? detectedFamily = GetFamily(detected);
        string? claimedFamily = GetFamily(claimed);

        return detectedFamily is not null && string.Equals(detectedFamily, claimedFamily, StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetFamily(string contentType)
    {
        int slash = contentType.IndexOf('/');
        return slash > 0 ? contentType[..slash] : null;
    }

    private bool IsAllowedContentType(string contentType)
    {
        foreach (string allowed in _config.AllowedContentTypes)
        {
            if (allowed.EndsWith("/*", StringComparison.Ordinal))
            {
                string prefix = allowed[..^1]; // "image/*" → "image/"
                if (contentType.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            else if (string.Equals(contentType, allowed, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
