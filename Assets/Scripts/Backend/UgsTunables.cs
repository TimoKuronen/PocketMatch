using System;
using System.IO;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Unity.Services.RemoteConfig;
using UnityEngine;
using VContainer.Unity;

/// <summary>
/// Serves tunables from Unity Remote Config. Starts from the last cached payload (or the baked-in defaults),
/// fetches in the background once signed in, and stores the result for the next launch.
/// </summary>
public sealed class UgsTunables : ITunables, IStartable
{
    private const string CacheFile = "tunables_cache.json";

    private struct UserAttributes { }
    private struct AppAttributes { }

    private readonly IUgsInitializer ugs;
    private TunableValues current = new TunableValues();

    private static string CachePath => Path.Combine(Application.persistentDataPath, CacheFile);

    public UgsTunables(IUgsInitializer ugs)
    {
        this.ugs = ugs;
    }

    public int ContinueCoinCost => current.continueCoinCost;
    public int ContinueExtraMoves => current.continueExtraMoves;
    public bool InterstitialsEnabled => current.interstitialsEnabled;
    public int Level1MoveLimit => current.level1MoveLimit;

    public void Start()
    {
        LoadCache();
        FetchAsync().Forget();
    }

    private async UniTaskVoid FetchAsync()
    {
        try
        {
            if (!await ugs.EnsureSignedInAsync())
                return;

            var config = await RemoteConfigService.Instance.FetchConfigsAsync(new UserAttributes(), new AppAttributes());
            var fetched = TunableValues.Read(key => RawValue(config.config, key), current);
            current = fetched;
            SaveCache(fetched);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Tunables] Remote Config fetch failed, keeping current values: {e.Message}");
        }
    }

    private static object RawValue(JObject config, string key)
    {
        return config != null && config.TryGetValue(key, out var token) && token is JValue value ? value.Value : null;
    }

    private void LoadCache()
    {
        try
        {
            if (!File.Exists(CachePath))
                return;

            var cached = JsonUtility.FromJson<TunableValues>(File.ReadAllText(CachePath));
            if (cached != null)
                current = TunableValues.Read(key => CachedValue(cached, key), current);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Tunables] Cache unreadable, using defaults: {e.Message}");
        }
    }

    private static object CachedValue(TunableValues cached, string key)
    {
        switch (key)
        {
            case TunableValues.ContinueCoinCostKey: return cached.continueCoinCost;
            case TunableValues.ContinueExtraMovesKey: return cached.continueExtraMoves;
            case TunableValues.InterstitialsEnabledKey: return cached.interstitialsEnabled;
            case TunableValues.Level1MoveLimitKey: return cached.level1MoveLimit;
            default: return null;
        }
    }

    private static void SaveCache(TunableValues values)
    {
        try
        {
            AtomicFile.Write(CachePath, JsonUtility.ToJson(values));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Tunables] Cache write failed: {e.Message}");
        }
    }
}
