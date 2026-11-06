using System;
using System.Collections.Generic;

/// <summary>Builds and sends the ad_watched and ad_skipped events. Analytics failures never reach ad flow.</summary>
public sealed class AdsAnalytics
{
    private readonly IAnalyticsService analytics;
    private readonly AdsLogger log;

    public AdsAnalytics(IAnalyticsService analytics, AdsLogger log)
    {
        this.analytics = analytics;
        this.log = log;
    }

    public void Watched(string adFormat, string placement, string result) =>
        Send(AnalyticsEvents.AdWatched, adFormat, placement, "result", result);

    public void Skipped(string adFormat, string placement, string reason) =>
        Send(AnalyticsEvents.AdSkipped, adFormat, placement, "reason", reason);

    private void Send(string eventName, string adFormat, string placement, string outcomeKey, string outcome)
    {
        try
        {
            analytics?.LogEvent(eventName, new Dictionary<string, object>
            {
                { "ad_format", adFormat },
                { "placement", placement },
                { outcomeKey, outcome }
            });
        }
        catch (Exception e)
        {
            log.Warn($"Failed to log event {eventName}: {e.Message}");
        }
    }
}
