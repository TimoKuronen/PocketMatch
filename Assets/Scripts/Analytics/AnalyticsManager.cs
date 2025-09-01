//#if FIREBASE_INSTALLED
using Firebase.Analytics;
//#endif
using System.Collections.Generic;
using UnityEngine;

public class AnalyticsManager : IAnalyticsManager
{
    public void Initialize()
    {

    }

    public void LogEvent(string eventName, Dictionary<string, object> parameters = null)
    {
        Debug.Log($"[AnalyticsManager] LogEvent: {eventName}");
        //#if FIREBASE_INSTALLED
        if (parameters == null || parameters.Count == 0)
        {
            FirebaseAnalytics.LogEvent(eventName);
        }
        else
        {
            var paramList = new List<Parameter>();
            foreach (var kv in parameters)
            {
                if (kv.Value is int intVal) paramList.Add(new Parameter(kv.Key, intVal));
                else if (kv.Value is long longVal) paramList.Add(new Parameter(kv.Key, longVal));
                else if (kv.Value is float floatVal) paramList.Add(new Parameter(kv.Key, floatVal));
                else if (kv.Value is double doubleVal) paramList.Add(new Parameter(kv.Key, doubleVal));
                else paramList.Add(new Parameter(kv.Key, kv.Value.ToString()));
            }
            FirebaseAnalytics.LogEvent(eventName, paramList.ToArray());
        }
//#else
   //     Debug.LogError("[FirebaseAnalyticsService] Firebase not installed.");
//#endif
    }

    public void Dispose()
    {
        throw new System.NotImplementedException();
    }
}
