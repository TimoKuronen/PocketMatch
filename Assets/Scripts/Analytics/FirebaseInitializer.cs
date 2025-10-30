using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class FirebaseInitializer : MonoBehaviour
{
    private IAnalyticsService analyticsService;

    [Inject]
    private void Construct(IAnalyticsService analyticsService)
    {
        this.analyticsService = analyticsService;

        analyticsService.LogEvent("session_started", new Dictionary<string, object>
        {
            { "device", SystemInfo.deviceModel },
            { "appVersion", Application.version }
        });
    }

    void OnApplicationQuit()
    {
        analyticsService?.LogEvent("session_ended", new Dictionary<string, object>
        {
            { "device", SystemInfo.deviceModel },
            { "appVersion", Application.version }
        });
    }
}
