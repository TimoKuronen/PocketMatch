using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirebaseInitializer : MonoBehaviour
{
    private IEnumerator Start()
    {
        Services.Get<IAnalyticsManager>().LogEvent("session_started", new Dictionary<string, object>
        {
            { "device", SystemInfo.deviceModel },
            { "appVersion", Application.version }
        });

        yield return null;
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
