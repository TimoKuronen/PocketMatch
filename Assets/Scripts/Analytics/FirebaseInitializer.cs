using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class FirebaseInitializer : IDisposable
{
    private IAnalyticsService analyticsService;

    [Inject]
    private void Construct(IAnalyticsService analyticsService)
    {
        Debug.Log("FirebaseInitializer Construct called.");

        this.analyticsService = analyticsService;

        analyticsService.LogEvent(AnalyticsEvents.SessionStarted, new Dictionary<string, object>
        {
            { "device", SystemInfo.deviceModel },
            { "app_version", Application.version }
        });
    }

    public void Dispose()
    {
        Debug.Log("FirebaseInitializer Dispose called.");

        analyticsService?.LogEvent(AnalyticsEvents.SessionEnded, new Dictionary<string, object>
        {
            { "device", SystemInfo.deviceModel },
            { "app_version", Application.version }
        });
    }
}
