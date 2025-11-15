namespace Umea.se.EstateService.Logic.Options;

public sealed class DataStoreRefreshOptions
{
    public const string SectionName = "EstateData";

    public double RefreshIntervalHours { get; init; } = 24d;
    public bool CacheEnabled { get; init; } = true;
    public string CacheFilePath { get; init; } = "DataStoreCache/estate-data.json";
    public bool AlwaysRefreshOnStartup { get; init; }

    public TimeSpan RefreshInterval
    {
        get
        {
            double hours = Math.Max(RefreshIntervalHours, 0d);
            return hours <= double.Epsilon
                ? TimeSpan.Zero
                : TimeSpan.FromHours(hours);
        }
    }
}
