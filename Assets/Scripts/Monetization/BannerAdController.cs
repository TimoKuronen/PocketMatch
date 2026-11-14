using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Services.LevelPlay;
using UnityEngine;

/// <summary>Banner lifecycle: create, load with retry, show, hide.</summary>
public sealed class BannerAdController : IDisposable
{
    private const string PlacementName = "Banner";

    private readonly string adUnitId;
    private readonly Func<bool> isSdkReady;
    private readonly AdsLogger log;
    private readonly CancellationToken token;
    private readonly AdsRetryPolicy retryPolicy = new();

    private LevelPlayBannerAd bannerAd;
    private bool isLoaded;
    private string lastError = "none";

    public BannerAdController(string adUnitId, Func<bool> isSdkReady, AdsLogger log, CancellationToken token)
    {
        this.adUnitId = adUnitId;
        this.isSdkReady = isSdkReady;
        this.log = log;
        this.token = token;
    }

    public string Describe() => $"Banner(hasAd={bannerAd != null}, loaded={isLoaded}, lastError={lastError})";

    public void CreateAd()
    {
        if (!isSdkReady())
        {
            log.Warn("Banner creation skipped: SDK not initialized.");
            return;
        }

        Cleanup();
        log.Verbose($"Creating banner (placement '{PlacementName}').");

        var config = new LevelPlayBannerAd.Config.Builder()
            .SetSize(LevelPlayAdSize.BANNER)
            .SetPosition(LevelPlayBannerPosition.BottomCenter)
            .SetDisplayOnLoad(false)
            .SetRespectSafeArea(false)
            .SetPlacementName(PlacementName)
            .Build();

        bannerAd = new LevelPlayBannerAd(adUnitId, config);
        bannerAd.OnAdLoaded += OnLoaded;
        bannerAd.OnAdLoadFailed += OnLoadFailed;
        bannerAd.OnAdDisplayFailed += OnDisplayFailed;
        bannerAd.OnAdClicked += OnClicked;
    }

    public void Show()
    {
        if (AdsDebugConfig.IsSimulating)
        {
            log.Verbose("Simulating banner display.");
            AdsDebugConfig.RaiseSimulatedBanner(true);
            return;
        }

        if (!isSdkReady())
        {
            log.Warn("ShowBannerAd ignored: SDK not initialized.");
            return;
        }

        // A fresh request starts a fresh retry budget.
        retryPolicy.Reset();

        if (bannerAd == null)
            CreateAd();

        if (bannerAd == null)
        {
            log.Warn("Banner could not be created; nothing to show.");
            return;
        }

        if (isLoaded)
        {
            bannerAd.ShowAd();
            return;
        }

        log.Verbose("Banner not loaded yet; requesting load.");
        bannerAd.LoadAd();
    }

    public void Hide()
    {
        if (AdsDebugConfig.IsSimulating)
            AdsDebugConfig.RaiseSimulatedBanner(false);

        if (!isSdkReady() || bannerAd == null)
            return;

        bannerAd.HideAd();
    }

    private void OnLoaded(LevelPlayAdInfo adInfo)
    {
        isLoaded = true;
        lastError = "none";
        retryPolicy.Reset();
        log.Verbose($"Banner loaded. {adInfo}");
        bannerAd?.ShowAd();
    }

    private void OnLoadFailed(LevelPlayAdError error)
    {
        isLoaded = false;
        lastError = error.ToString();
        var kind = AdsRetryPolicy.Classify(error.ToString());

        if (kind == AdsRetryPolicy.FailureKind.InvalidAdUnit)
        {
            log.Warn(
                $"Banner load failed: invalid ad unit id (626). Check that the banner unit is active, BANNER type, " +
                $"linked to bundle '{Application.identifier}', and enabled for Android. Not retrying. {error}");
            retryPolicy.Reset();
            return;
        }

        if (!retryPolicy.TryGetRetryDelay(kind, out float delay))
        {
            log.Warn($"Banner load failed ({kind}); retries exhausted. {error}");
            retryPolicy.Reset();
            return;
        }

        log.Warn($"Banner load failed ({kind}); retrying in {delay:0}s. {error}");
        RetryLoadAsync(delay).Forget();
    }

    private void OnDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        isLoaded = false;
        log.Error($"Banner display failed: {error}. {adInfo}");
    }

    private void OnClicked(LevelPlayAdInfo adInfo)
    {
        log.Verbose($"Banner clicked. {adInfo}");
    }

    private async UniTaskVoid RetryLoadAsync(float delay)
    {
        bool cancelled = await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: token)
            .SuppressCancellationThrow();

        if (!cancelled && bannerAd != null && isSdkReady() && !isLoaded)
            bannerAd.LoadAd();
    }

    private void Cleanup()
    {
        if (bannerAd == null)
            return;

        bannerAd.OnAdLoaded -= OnLoaded;
        bannerAd.OnAdLoadFailed -= OnLoadFailed;
        bannerAd.OnAdDisplayFailed -= OnDisplayFailed;
        bannerAd.OnAdClicked -= OnClicked;
        bannerAd = null;
    }

    public void Dispose() => Cleanup();
}
