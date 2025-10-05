using Firebase;
using Firebase.Analytics;
using Firebase.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class AnalyticsManager : IAnalyticsManager
{
    private const string CacheFile = "analytics_cache.json";
    private List<CachedEvent> eventQueue = new List<CachedEvent>();
    private bool firebaseReady = false;

    public void Initialize()
    {
        LoadCache();

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var status = task.Result;
            if (status == DependencyStatus.Available)
            {
                firebaseReady = true;
                FirebaseAnalytics.LogEvent("app_started");
                FlushEvents();
            }
            else
            {
                Debug.LogError("Firebase dependencies not resolved: " + status);
            }
        });

        Application.focusChanged += OnAppFocusChanged;
        Application.quitting += OnAppQuit;
    }

    public void LogEvent(string eventName, Dictionary<string, object> parameters = null)
    {
        var newEvent = new CachedEvent
        {
            EventName = eventName,
            Parameters = parameters ?? new Dictionary<string, object>(),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        eventQueue.Add(newEvent);
        SaveCache();

        if (firebaseReady)
        {
            TrySendEvent(newEvent);
        }
    }

    private void TrySendEvent(CachedEvent cachedEvent)
    {
        try
        {
            if (cachedEvent.Parameters == null || cachedEvent.Parameters.Count == 0)
            {
                FirebaseAnalytics.LogEvent(cachedEvent.EventName);
            }
            else
            {
                var paramList = new List<Parameter>();
                foreach (var kv in cachedEvent.Parameters)
                {
                    if (kv.Value is int intVal) paramList.Add(new Parameter(kv.Key, intVal));
                    else if (kv.Value is long longVal) paramList.Add(new Parameter(kv.Key, longVal));
                    else if (kv.Value is float floatVal) paramList.Add(new Parameter(kv.Key, floatVal));
                    else if (kv.Value is double doubleVal) paramList.Add(new Parameter(kv.Key, doubleVal));
                    else paramList.Add(new Parameter(kv.Key, kv.Value.ToString()));
                }
                FirebaseAnalytics.LogEvent(cachedEvent.EventName, paramList.ToArray());
            }

            eventQueue.Remove(cachedEvent);
            SaveCache();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AnalyticsManager] Failed to send event {cachedEvent.EventName}: {e.Message}");
        }
    }

    private void FlushEvents()
    {
        if (!firebaseReady) 
            return;

        var eventsCopy = new List<CachedEvent>(eventQueue);
        foreach (var e in eventsCopy)
        {
            TrySendEvent(e);
        }
    }

    private void LoadCache()
    {
        string path = Path.Combine(Application.persistentDataPath, CacheFile);
        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                eventQueue = JsonConvert.DeserializeObject<List<CachedEvent>>(json) ?? new List<CachedEvent>();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AnalyticsManager] Failed to load cache: {e.Message}");
                eventQueue = new List<CachedEvent>();
            }
        }
    }

    private void SaveCache()
    {
        string path = Path.Combine(Application.persistentDataPath, CacheFile);
        try
        {
            string json = JsonConvert.SerializeObject(eventQueue, Formatting.Indented);
            File.WriteAllText(path, json);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AnalyticsManager] Failed to save cache: {e.Message}");
        }
    }

    private void OnAppFocusChanged(bool hasFocus)
    {
        if (!hasFocus) 
            SaveCache();
    }

    private void OnAppQuit()
    {
        SaveCache();
    }

    public void Dispose()
    {
        SaveCache();
    }
}

[Serializable]
public class CachedEvent
{
    public string EventName;
    public Dictionary<string, object> Parameters;
    public long Timestamp;
}