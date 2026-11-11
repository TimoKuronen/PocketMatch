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
/// <see cref="IAnalyticsTransport"/>, holding events in a local queue until the transport is ready.
/// With a disabled transport nothing is queued or stored; events are only logged in debug builds.
/// </summary>
public class AnalyticsService : IAnalyticsService, IStartable, IDisposable
{
    private const string CacheFile = "analytics_cache.json";

    private List<CachedEvent> eventQueue = new List<CachedEvent>();
    private bool transportReady;
    private ISaveService saveService;
    private IEconomyService economyService;
    private IAnalyticsTransport transport;

    [Inject]
    public void Construct(ISaveService saveService, IEconomyService economyService, IAnalyticsTransport transport)
    {
        this.saveService = saveService;
        this.economyService = economyService;
        this.transport = transport;

        if (!transport.IsEnabled)
            return;

        LoadCache();
        InitializeTransportAsync().Forget();

        Application.focusChanged += OnAppFocusChanged;
        Application.quitting += SaveCache;
    }

    public void Start()
    {
        LevelEvents.OnLevelStarted += OnLevelStarted;
        LevelEvents.OnLevelCompleted += OnLevelCompleted;
        LevelEvents.OnLevelFailed += OnLevelFailed;
        economyService.OnCoinsSpent += OnCoinsSpent;

        LogEvent(AnalyticsEvents.AppStarted);
        LogEvent(AnalyticsEvents.SessionStarted, CreateSessionParameters());
    }

    public void Dispose()
    {
        LogEvent(AnalyticsEvents.SessionEnded, CreateSessionParameters());

        LevelEvents.OnLevelStarted -= OnLevelStarted;
        LevelEvents.OnLevelCompleted -= OnLevelCompleted;
        LevelEvents.OnLevelFailed -= OnLevelFailed;
        economyService.OnCoinsSpent -= OnCoinsSpent;

        if (!transport.IsEnabled)
            return;

        Application.focusChanged -= OnAppFocusChanged;
        Application.quitting -= SaveCache;
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

        eventQueue.Add(newEvent);
        SaveCache();

        if (transportReady)
            TrySendEvent(newEvent);
    }

    private static Dictionary<string, object> CreateSessionParameters()
    {
        return new Dictionary<string, object>
        {
            { "device", SystemInfo.deviceModel },
            { "app_version", Application.version }
        };
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

        // Mirrors the EconomyService coin grant on level_complete wins.
        if (e.TotalScore > 0)
        {
            LogEvent(AnalyticsEvents.CoinsEarned, new Dictionary<string, object>
            {
                { "amount", e.TotalScore },
                { "source", "level_complete" },
                { "level_name", e.LevelName }
            });
        }
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
        try
        {
            transportReady = await transport.InitializeAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Analytics] Transport initialization failed: {e.Message}");
            return;
        }

        if (transportReady)
            FlushEvents();
    }

    private void TrySendEvent(CachedEvent cachedEvent)
    {
        try
        {
            transport.Send(cachedEvent.EventName, cachedEvent.Parameters);
            eventQueue.Remove(cachedEvent);
            SaveCache();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Analytics] Failed to send event {cachedEvent.EventName}: {e.Message}");
        }
    }

    private void FlushEvents()
    {
        var eventsCopy = new List<CachedEvent>(eventQueue);
        foreach (var e in eventsCopy)
            TrySendEvent(e);
    }

    private void LoadCache()
    {
        string path = Path.Combine(Application.persistentDataPath, CacheFile);
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
        string path = Path.Combine(Application.persistentDataPath, CacheFile);
        try
        {
            string json = JsonConvert.SerializeObject(eventQueue, Formatting.Indented);
            File.WriteAllText(path, json);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Analytics] Failed to save cache: {e.Message}");
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
