using System.Collections.Generic;
using UnityEngine;

public class FirebaseInitializer : MonoBehaviour
{
    private void Awake()
    {
        Services.Get<IAnalyticsManager>().LogEvent("session_started", new Dictionary<string, object>
        {
            { "device", SystemInfo.deviceModel },
            { "appVersion", Application.version }
        });
    }

    void OnApplicationQuit()
    {
        Services.Get<IAnalyticsManager>().LogEvent("session_ended", new Dictionary<string, object>
        {
            { "device", SystemInfo.deviceModel },
            { "appVersion", Application.version }
        });
    }
}
