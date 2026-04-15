namespace Umea.se.EstateService.Shared.Infrastructure.ConfigurationModels;

public class WorkOrderConfiguration
{
    /// <summary>
    /// File storage location. Can be:
    /// <list type="bullet">
    ///   <item>A local path (e.g. "./workorder-files") — uses local filesystem</item>
    ///   <item>A blob service URL (e.g. "https://account.blob.core.windows.net") — uses Azure Blob Storage with DefaultAzureCredential</item>
    ///   <item>A connection string (e.g. "UseDevelopmentStorage=true") — uses Azure Blob Storage with connection string</item>
    /// </list>
    /// </summary>
    public string FileStorage { get; set; } = "./workorder-files";

    /// <summary>Blob container name. Only used when FileStorage points to blob storage.</summary>
    public string FileStorageContainer { get; set; } = "workorder-files";

    public int ProcessingIntervalSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 5;
    public int RetryBaseDelaySeconds { get; set; } = 60;
    public int StatusCheckIntervalMinutes { get; set; } = 60;
    public int ProcessingTimeoutMinutes { get; set; } = 10;

    public int? DocumentActionTypeId { get; set; }
    public int? DocumentActionTypeStatusId { get; set; }

    public WorkOrderFileValidationConfig FileValidation { get; set; } = new();

    public FileStorageType ResolvedStorageType => FileStorage switch
    {
        var s when s.StartsWith("https://", StringComparison.OrdinalIgnoreCase) => FileStorageType.BlobUrl,
        var s when s.Contains("AccountName=", StringComparison.OrdinalIgnoreCase)
               || s.Contains("UseDevelopmentStorage=", StringComparison.OrdinalIgnoreCase) => FileStorageType.BlobConnectionString,
        _ => FileStorageType.LocalFileSystem
    };
}

public class WorkOrderFileValidationConfig
{
    public int MaxFileCount { get; set; } = 10;
    public long MaxFileSizeBytes { get; set; } = 20 * 1024 * 1024; // 20 MB

    public List<string> AllowedContentTypes { get; set; } = [];
}

public enum FileStorageType
{
    LocalFileSystem,
    BlobUrl,
    BlobConnectionString
}
