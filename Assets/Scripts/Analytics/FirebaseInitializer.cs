using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class FirebaseInitializer
{
    private IAnalyticsService analyticsService;

    [Inject]
    private void Construct(IAnalyticsService analyticsService)
    {
        Debug.Log("FirebaseInitializer Construct called.");

        this.analyticsService = analyticsService;

        analyticsService.LogEvent("session_started", new Dictionary<string, object>
        {
            { "device", SystemInfo.deviceModel },
            { "appVersion", Application.version }
        });
    }

    public void Dispose()
    {
        Debug.Log("FirebaseInitializer Dispose called.");

        analyticsService?.LogEvent("session_ended", new Dictionary<string, object>
        {
            { "device", SystemInfo.deviceModel },
            { "appVersion", Application.version }
        });
    }
}
