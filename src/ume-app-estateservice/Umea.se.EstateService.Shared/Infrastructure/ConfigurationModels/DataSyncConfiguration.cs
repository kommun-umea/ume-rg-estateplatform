namespace Umea.se.EstateService.Shared.Infrastructure.ConfigurationModels;

public class DataSyncConfiguration
{
    /// <summary>
    /// Cron expression for the refresh schedule (5-field standard format).
    /// Examples: "0 2 * * 0" = every Sunday 02:00, "0 2 * * *" = every day 02:00.
    /// When null or empty, no scheduled refresh runs — only manual triggers and startup refresh.
    /// </summary>
    public string? Schedule { get; set; }

    /// <summary>
    /// IANA time zone for <see cref="Schedule"/>. Defaults to "Europe/Stockholm".
    /// </summary>
    public string TimeZone { get; set; } = "Europe/Stockholm";

    public int MaxRetries { get; set; } = 5;
    public int RetryBaseDelaySeconds { get; set; } = 10;
}
