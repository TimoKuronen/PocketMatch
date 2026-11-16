using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// Maps gameplay and economy events onto the analytics dictionary and delivers them through
/// <see cref="IAnalyticsTransport"/>. Events wait in a capped local queue until the transport is ready.
/// While the transport is disabled nothing is queued or stored, and anything already queued is discarded;
/// events are only logged in debug builds.
/// </summary>
public class AnalyticsService : IAnalyticsService, IStartable, IDisposable
{
    private const string CacheFile = "analytics_cache.json";
    private const int MaxQueuedEvents = 200;

    private List<CachedEvent> eventQueue = new List<CachedEvent>();
    private bool transportReady;
    private bool active;
    private ISaveService saveService;
    private IEconomyService economyService;
    private IAnalyticsTransport transport;

    private static string CachePath => Path.Combine(Application.persistentDataPath, CacheFile);

    [Inject]
    public void Construct(ISaveService saveService, IEconomyService economyService, IAnalyticsTransport transport)
    {
        this.saveService = saveService;
        this.economyService = economyService;
        this.transport = transport;

        transport.EnabledChanged += OnTransportEnabledChanged;
        if (transport.IsEnabled)
            Activate();
    }

    public void Start()
    {
        LevelEvents.OnLevelStarted += OnLevelStarted;
        LevelEvents.OnLevelCompleted += OnLevelCompleted;
        LevelEvents.OnLevelFailed += OnLevelFailed;
        economyService.OnCoinsSpent += OnCoinsSpent;
    }

    public void Dispose()
    {
        LevelEvents.OnLevelStarted -= OnLevelStarted;
        LevelEvents.OnLevelCompleted -= OnLevelCompleted;
        LevelEvents.OnLevelFailed -= OnLevelFailed;
        economyService.OnCoinsSpent -= OnCoinsSpent;
        transport.EnabledChanged -= OnTransportEnabledChanged;

        if (!active)
            return;

        Deactivate();
        SaveCache();
    }

    public void LogEvent(string eventName, Dictionary<string, object> parameters = null)
    {
        LogToConsole(eventName, parameters);

        if (!transport.IsEnabled)
            return;

        var newEvent = new CachedEvent
        {
            EventName = eventName,
            Parameters = parameters ?? new Dictionary<string, object>(),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        if (transportReady)
        {
            TrySendEvent(newEvent);
            return;
        }

        Enqueue(newEvent);
    }

    private void Activate()
    {
        if (active)
            return;

        active = true;
        LoadCache();
        InitializeTransportAsync().Forget();

        Application.focusChanged += OnAppFocusChanged;
        Application.quitting += SaveCache;
    }

    private void Deactivate()
    {
        active = false;
        transportReady = false;

        Application.focusChanged -= OnAppFocusChanged;
        Application.quitting -= SaveCache;
    }

    private void OnTransportEnabledChanged()
    {
        if (transport.IsEnabled)
        {
            Activate();
            return;
        }

        if (!active)
            return;

        Deactivate();
        eventQueue.Clear();
        DeleteCache();
    }

    private void Enqueue(CachedEvent cachedEvent)
    {
        eventQueue.Add(cachedEvent);
        if (eventQueue.Count > MaxQueuedEvents)
            eventQueue.RemoveRange(0, eventQueue.Count - MaxQueuedEvents);

        SaveCache();
    }

    private static void LogToConsole(string eventName, Dictionary<string, object> parameters)
    {
        if (!Debug.isDebugBuild)
            return;

        var message = new StringBuilder("[Analytics] ").Append(eventName);
        if (parameters != null)
        {
            foreach (var parameter in parameters)
                message.Append(' ').Append(parameter.Key).Append('=').Append(parameter.Value);
        }

        Debug.Log(message.ToString());
    }

    private void OnCoinsSpent(CoinsSpentEventArgs e)
    {
        int levelIndex = GameSignals.ActiveLevelIndex >= 0
            ? GameSignals.ActiveLevelIndex + 1
            : saveService.PlayerData.nextLevelIndex + 1;

        LogEvent(AnalyticsEvents.CoinsSpent, new Dictionary<string, object>
        {
            { "amount", e.Amount },
            { "reason", e.Reason },
            { "level_index", levelIndex }
        });
    }

    private void OnLevelStarted(object sender, LevelStartedEventArgs e)
    {
        int levelIndex = GameSignals.ActiveLevelIndex >= 0
            ? GameSignals.ActiveLevelIndex + 1
            : saveService.PlayerData.nextLevelIndex + 1;

        LogEvent(AnalyticsEvents.LevelStarted, new Dictionary<string, object>
        {
            { "level_name", e.LevelName },
            { "level_index", levelIndex }
        });
    }

    private void OnLevelCompleted(object sender, LevelCompletedEventArgs e)
    {
        LogEvent(AnalyticsEvents.LevelCompleted, new Dictionary<string, object>
        {
            { "level_name", e.LevelName },
            { "moves_spent", e.MovesSpent },
            { "total_score", e.TotalScore },
            { "match_duration_sec", e.GameTimeInSeconds }
        });
    }

    private void OnLevelFailed(object sender, LevelFailedEventArgs e)
    {
        LogEvent(AnalyticsEvents.LevelFailed, new Dictionary<string, object>
        {
            { "level_name", e.LevelName },
            { "match_duration_sec", e.GameTimeInSeconds }
        });
    }

    private async UniTaskVoid InitializeTransportAsync()
    {
        bool ready;
        try
        {
            ready = await transport.InitializeAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Analytics] Transport initialization failed: {e.Message}");
            return;
        }

        if (!ready || !active)
            return;

        transportReady = true;
        FlushEvents();
    }

    private void TrySendEvent(CachedEvent cachedEvent)
    {
        try
        {
            transport.Send(cachedEvent.EventName, cachedEvent.Parameters);
            eventQueue.Remove(cachedEvent);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Analytics] Failed to send event {cachedEvent.EventName}: {e.Message}");
            if (!eventQueue.Contains(cachedEvent))
                Enqueue(cachedEvent);
        }
    }

    private void FlushEvents()
    {
        var eventsCopy = new List<CachedEvent>(eventQueue);
        foreach (var e in eventsCopy)
            TrySendEvent(e);

        SaveCache();
    }

    private void LoadCache()
    {
        string path = CachePath;
        if (!File.Exists(path))
            return;

        try
        {
            string json = File.ReadAllText(path);
            eventQueue = JsonConvert.DeserializeObject<List<CachedEvent>>(json) ?? new List<CachedEvent>();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Analytics] Failed to load cache: {e.Message}");
            eventQueue = new List<CachedEvent>();
        }
    }

    private void SaveCache()
    {
        try
        {
            string json = JsonConvert.SerializeObject(eventQueue, Formatting.Indented);
            File.WriteAllText(CachePath, json);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Analytics] Failed to save cache: {e.Message}");
        }
    }

    private static void DeleteCache()
    {
        try
        {
            File.Delete(CachePath);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Analytics] Failed to delete cache: {e.Message}");
        }
    }

    private void OnAppFocusChanged(bool hasFocus)
    {
        if (!hasFocus)
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
