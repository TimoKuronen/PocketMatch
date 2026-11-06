using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Services.LevelPlay;

/// <summary>Interstitial lifecycle: create, preload, show, reload after close or load failure.</summary>
public sealed class InterstitialAdController : IDisposable
{
    private const string AdFormat = "interstitial";
    private const string PlacementName = "interstitial";
    private const float ReloadDelaySeconds = 3f;

    private readonly string adUnitId;
    private readonly Func<bool> isSdkReady;
    private readonly Action hideBanner;
    private readonly AdsAnalytics analytics;
    private readonly AdsLogger log;
    private readonly CancellationToken token;

    private LevelPlayInterstitialAd interstitialAd;

    public event Action Closed;

    public bool IsReady => interstitialAd?.IsAdReady() ?? false;

    /// <summary>True once the current show has ended (closed or failed); false while one is in flight.</summary>
    public bool Completed { get; private set; }

    public InterstitialAdController(
        string adUnitId,
        Func<bool> isSdkReady,
        Action hideBanner,
        AdsAnalytics analytics,
        AdsLogger log,
        CancellationToken token)
    {
        this.adUnitId = adUnitId;
        this.isSdkReady = isSdkReady;
        this.hideBanner = hideBanner;
        this.analytics = analytics;
        this.log = log;
        this.token = token;
    }

    public string Describe() => $"Interstitial(hasAd={interstitialAd != null}, ready={IsReady}, completed={Completed})";

    public void MarkComplete() => Completed = true;

    public void CreateAd()
    {
        if (!isSdkReady())
        {
            log.Warn("Interstitial creation skipped: SDK not initialized.");
            return;
        }

        Cleanup();

        try
        {
            interstitialAd = new LevelPlayInterstitialAd(adUnitId);
            interstitialAd.OnAdLoaded += OnLoaded;
            interstitialAd.OnAdClosed += OnClosed;
            interstitialAd.OnAdDisplayed += OnDisplayed;
            interstitialAd.OnAdClicked += OnClicked;
            interstitialAd.OnAdLoadFailed += OnLoadFailed;
            interstitialAd.OnAdDisplayFailed += OnDisplayFailed;

            Load();
        }
        catch (Exception e)
        {
            log.Error($"Interstitial creation threw: {e}");
        }
    }

    public void Show()
    {
        // The Editor placeholder ad can fail to fire close, so the Editor completes the show immediately.
#if UNITY_EDITOR
        log.Verbose("Simulating interstitial completion in Editor.");
        Completed = true;
        analytics.Watched(AdFormat, PlacementName, "editor_simulated");
        Closed?.Invoke();
#else
        if (!isSdkReady() || interstitialAd == null)
        {
            log.Warn($"ShowInterstitialAd ignored (initialized={isSdkReady()}, hasAd={interstitialAd != null}).");
            analytics.Skipped(AdFormat, PlacementName, "not_initialized");
            return;
        }

        hideBanner();

        if (!interstitialAd.IsAdReady())
        {
            log.Warn("Interstitial not ready; requesting load instead.");
            analytics.Skipped(AdFormat, PlacementName, "not_ready");
            Load();
            return;
        }

        Completed = false;
        interstitialAd.ShowAd(PlacementName);
#endif
    }

    private void Load()
    {
        if (interstitialAd == null)
        {
            log.Warn("Interstitial load requested before the ad existed; recreating.");
            CreateAd();
            return;
        }

        log.Verbose("Requesting interstitial load.");
        interstitialAd.LoadAd();
    }

    private void OnLoaded(LevelPlayAdInfo adInfo)
    {
        log.Verbose($"Interstitial loaded. {adInfo}");
    }

    private void OnDisplayed(LevelPlayAdInfo adInfo)
    {
        log.Verbose($"Interstitial displayed. {adInfo}");
    }

    private void OnClicked(LevelPlayAdInfo adInfo)
    {
        log.Verbose($"Interstitial clicked. {adInfo}");
    }

    private void OnClosed(LevelPlayAdInfo adInfo)
    {
        Completed = true;
        log.Verbose($"Interstitial closed. {adInfo}");
        analytics.Watched(AdFormat, PlacementName, "completed");
        Closed?.Invoke();
        Load();
    }

    private void OnLoadFailed(LevelPlayAdError error)
    {
        log.Warn($"Interstitial load failed; retrying in {ReloadDelaySeconds:0}s. {error}");
        ReloadAfterDelayAsync().Forget();
    }

    private void OnDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        log.Error($"Interstitial display failed: {error}. {adInfo}");
        analytics.Skipped(AdFormat, PlacementName, "display_failed");
        Completed = true;
        Closed?.Invoke();
    }

    private async UniTaskVoid ReloadAfterDelayAsync()
    {
        bool cancelled = await UniTask.Delay(TimeSpan.FromSeconds(ReloadDelaySeconds), cancellationToken: token)
            .SuppressCancellationThrow();

        if (!cancelled)
            Load();
    }

    private void Cleanup()
    {
        if (interstitialAd == null)
            return;

        interstitialAd.OnAdLoaded -= OnLoaded;
        interstitialAd.OnAdClosed -= OnClosed;
        interstitialAd.OnAdDisplayed -= OnDisplayed;
        interstitialAd.OnAdClicked -= OnClicked;
        interstitialAd.OnAdLoadFailed -= OnLoadFailed;
        interstitialAd.OnAdDisplayFailed -= OnDisplayFailed;
        interstitialAd = null;
    }

    public void Dispose() => Cleanup();
}
