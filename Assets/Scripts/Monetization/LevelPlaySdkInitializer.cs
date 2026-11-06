using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Services.LevelPlay;
using UnityEngine;

/// <summary>Owns LevelPlay SDK init: offline wait, one-time event subscription, and retry after failure.</summary>
public sealed class LevelPlaySdkInitializer : IDisposable
{
    private const float StartDelaySeconds = 0.5f;
    private const float OfflineRecheckSeconds = 3f;
    private const float InitRetryDelaySeconds = 5f;

    private readonly string appKey;
    private readonly AdsLogger log;
    private readonly CancellationToken token;

    private bool started;

    public bool IsInitialized { get; private set; }

    public event Action Initialized;

    public LevelPlaySdkInitializer(string appKey, AdsLogger log, CancellationToken token)
    {
        this.appKey = appKey;
        this.log = log;
        this.token = token;
    }

    public void Start()
    {
        if (started)
            return;

        started = true;
        StartAsync().Forget();
    }

    private async UniTask StartAsync()
    {
        // Gives Unity Services a moment to come up before touching the SDK.
        await UniTask.Delay(TimeSpan.FromSeconds(StartDelaySeconds), cancellationToken: token);

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            log.Warn($"No internet connection. Rechecking in {OfflineRecheckSeconds:0}s.");
            await UniTask.Delay(TimeSpan.FromSeconds(OfflineRecheckSeconds), cancellationToken: token);

            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                log.Error("Still offline. LevelPlay will not initialize this session.");
                return;
            }
        }

        log.Verbose($"Initializing LevelPlay. Platform={Application.platform}, Bundle={Application.identifier}");

        LevelPlay.ValidateIntegration();
        LevelPlay.OnInitSuccess += OnInitSuccess;
        LevelPlay.OnInitFailed += OnInitFailed;
        TryInit();
    }

    private void TryInit()
    {
        try
        {
            LevelPlay.Init(appKey);
        }
        catch (Exception e)
        {
            log.Error($"LevelPlay.Init threw: {e}");
        }
    }

    private void OnInitSuccess(LevelPlayConfiguration configuration)
    {
        IsInitialized = true;
        log.Info("LevelPlay initialized.");
        log.Verbose($"Configuration: {configuration}");
        Initialized?.Invoke();
    }

    private void OnInitFailed(LevelPlayInitError error)
    {
        log.Error($"LevelPlay init failed: {error}");

        if (!IsInitialized)
            RetryInitAsync().Forget();
    }

    private async UniTask RetryInitAsync()
    {
        log.Verbose($"Scheduling init retry in {InitRetryDelaySeconds:0}s.");
        await UniTask.Delay(TimeSpan.FromSeconds(InitRetryDelaySeconds), cancellationToken: token);

        if (!IsInitialized && Application.internetReachability != NetworkReachability.NotReachable)
            TryInit();
    }

    public void Dispose()
    {
        LevelPlay.OnInitSuccess -= OnInitSuccess;
        LevelPlay.OnInitFailed -= OnInitFailed;
    }
}
