using System;
using System.Collections.Generic;

/// <summary>Builds and sends the ad impression event. Analytics failures never reach ad flow.</summary>
public sealed class AdsAnalytics
{
    private readonly IAnalyticsService analytics;
    private readonly AdsLogger log;

    public AdsAnalytics(IAnalyticsService analytics, AdsLogger log)
    {
        this.analytics = analytics;
        this.log = log;
    }

    public void Watched(string adFormat, string placement, string result)
    {
        try
        {
            analytics?.LogEvent(AnalyticsEvents.AdImpression, new Dictionary<string, object>
            {
                { "ad_format", adFormat },
                { "placement", placement },
                { "result", result }
            });
        }
        catch (Exception e)
        {
            log.Warn($"Failed to log event {AnalyticsEvents.AdImpression}: {e.Message}");
        }
    }
}
