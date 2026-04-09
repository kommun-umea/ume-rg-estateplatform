namespace Umea.se.EstateService.Shared.Infrastructure.ConfigurationModels;

public class DataSyncConfiguration
{
    public SyncScheduleConfiguration Schedule { get; set; } = new();

    /// <summary>
    /// IANA time zone for schedule expressions. Defaults to "Europe/Stockholm".
    /// </summary>
    public string TimeZone { get; set; } = "Europe/Stockholm";

    public int MaxRetries { get; set; } = 5;
    public int RetryBaseDelaySeconds { get; set; } = 10;
}

public class SyncScheduleConfiguration
{
    /// <summary>
    /// Cron expression for the core data refresh (5-field standard format).
    /// Examples: "0 2 * * 0" = every Sunday 02:00, "0 2 * * *" = every day 02:00.
    /// Other sync types fall back to this when their own cron is null.
    /// </summary>
    public string? Default { get; set; }

    /// <summary>
    /// Cron override for document sync. Null falls back to <see cref="Default"/>.
    /// </summary>
    public string? Documents { get; set; }

    /// <summary>
    /// Cron for image pre-warm. Null = disabled (no fallback to Default).
    /// Image pre-warm is an optimization, not a data sync — must be explicitly enabled.
    /// </summary>
    public string? Images { get; set; }

    /// <summary>Resolve the effective cron for a supplementary sync type.</summary>
    public string? Resolve(SyncType type) => type switch
    {
        SyncType.Documents => Documents ?? Default,
        SyncType.Images => Images,  // No fallback — opt-in only
        _ => Default
    };
}

public enum SyncType
{
    Documents,
    Images
}
