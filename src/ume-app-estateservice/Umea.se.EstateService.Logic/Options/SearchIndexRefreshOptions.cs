namespace Umea.se.EstateService.Logic.Options;

public sealed class SearchIndexRefreshOptions
{
    public const string SectionName = "SearchIndex";

    public double RefreshIntervalHours { get; init; } = 4d;

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
