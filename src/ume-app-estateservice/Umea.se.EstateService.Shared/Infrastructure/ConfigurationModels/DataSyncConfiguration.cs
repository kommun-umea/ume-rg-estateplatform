namespace Umea.se.EstateService.Shared.Infrastructure.ConfigurationModels;

public class DataSyncConfiguration
{
    public double RefreshIntervalHours { get; set; } = 24;
    public bool AlwaysRefreshOnStartup { get; set; }
    public bool CacheEnabled { get; set; } = true;
    public string CacheFilePath { get; set; } = "DataStoreCache/estate-data.json";
    public int MaxRetries { get; set; } = 5;
    public int RetryBaseDelaySeconds { get; set; } = 10;

    public TimeSpan RefreshInterval => TimeSpan.FromHours(Math.Max(RefreshIntervalHours, 0));
}
