namespace Umea.se.Toolkit.Images.Caching;

/// <summary>
/// Configuration options for Azure Blob Storage cache.
/// Supports both connection string and Managed Identity authentication.
/// </summary>
public sealed class BlobCacheOptions
{
    /// <summary>
    /// Azure Blob Storage connection string. Takes precedence over ServiceUri if both are set.
    /// Use for local development or when Managed Identity is not available.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Blob service URI for Managed Identity authentication (e.g., "https://mystorageaccount.blob.core.windows.net").
    /// Used when ConnectionString is not set. Requires DefaultAzureCredential to be available.
    /// </summary>
    public Uri? ServiceUri { get; set; }

    /// <summary>
    /// Container name for cached data. Default: "cache".
    /// </summary>
    public string ContainerName { get; set; } = "cache";

    /// <summary>
    /// Create container on startup if it doesn't exist. Default: false.
    /// Set to true only for local development; in deployed environments the container is provisioned by infrastructure.
    /// </summary>
    public bool CreateContainerIfNotExists { get; set; }

    /// <summary>
    /// Returns true if blob cache is properly configured (either connection string or service URI).
    /// </summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ConnectionString) || ServiceUri is not null;

    /// <summary>
    /// Returns true if using Managed Identity (ServiceUri without ConnectionString).
    /// </summary>
    public bool UseManagedIdentity => string.IsNullOrWhiteSpace(ConnectionString) && ServiceUri is not null;
}
