using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Services.LevelPlay;
using UnityEngine;
using VContainer;

public class AdsService : IAdsService, IDisposable
{
    private const string AppKey = "23b074f85";
    private const string BannerAdId = "dq0x680o73ez3iyt";
    private const string InterstitialAdId = "adgl52j6cwsc0pge";
    
    private const string BannerPlacementName = "Banner";
    private const string InterstitialPlacementName = "interstitial";

    private LevelPlayBannerAd bannerAd;
    private LevelPlayInterstitialAd interstitialAd;
    private IAnalyticsService analyticsService;
    private bool isBannerLoaded = false;
    private int bannerRetryCount = 0;
    private const int MaxBannerRetries = 3;
    private int bannerNoFillRetryCount = 0;
    private const int MaxBannerNoFillRetries = 3;
    private const float NoFillRetryDelaySeconds = 15f;

    public event Action OnInterstitialAdClosed;

    public bool IsInitialized { get; private set; } = false;
    public bool InterstitialAdReady => interstitialAd?.IsAdReady() ?? false;
    public bool InterstitialAdCompleted { get; private set; } = false;

    private readonly CancellationTokenSource cts = new();

    #region Initialization

    [Inject]
    public void Construct(IAnalyticsService analyticsService)
    {
        this.analyticsService = analyticsService;
        Debug.Log("[AdsService] Constructed. Starting delayed LevelPlay initialization.");
        DelayedInitAsync().Forget();
    }

    private async UniTaskVoid DelayedInitAsync()
    {
        var token = cts.Token;
        // Wait a bit longer to ensure Unity Services are ready
        await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: token);
        
        // Check network connectivity before initializing
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            Debug.LogWarning("[AdsService] No internet connection. Retrying initialization in 3 seconds...");
            await UniTask.Delay(TimeSpan.FromSeconds(3f), cancellationToken: token);
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                Debug.LogError("[AdsService] Still no internet connection. Cannot initialize ads.");
                return;
            }
        }

        Debug.Log($"[AdsService] Initializing LevelPlay SDK with AppKey: {AppKey} on platform: {Application.platform}");
        Debug.Log($"[AdsService] Bundle ID: {Application.identifier}");
        Debug.Log($"[AdsService] Internet reachability: {Application.internetReachability}");
        
        LevelPlay.ValidateIntegration();
        Debug.Log("[AdsService] Integration validation requested.");
        LevelPlay.OnInitSuccess += OnSdkInitSuccess;
        LevelPlay.OnInitFailed += OnSdkInitFailed;
        
        try
        {
            Debug.Log("[AdsService] Calling LevelPlay.Init...");
            LevelPlay.Init(AppKey);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AdsService] Exception during LevelPlay.Init: {e}");
        }
    }

    private void OnSdkInitFailed(LevelPlayInitError error)
    {
        Debug.LogError($"[AdsService] SDK Initialization Failed: {error}");
        LogAdsState("Init failed");
        
        // Retry initialization after a delay (but only once to avoid spam)
        if (!IsInitialized)
        {
            RetryInitAsync(5f).Forget();
        }
    }
    
    private async UniTask RetryInitAsync(float delay)
    {
        var token = cts.Token;
        Debug.Log($"[AdsService] Scheduling SDK init retry in {delay:0.0}s");
        await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: token);
        
        if (!IsInitialized && Application.internetReachability != NetworkReachability.NotReachable)
        {
            Debug.Log("[AdsService] Retrying SDK initialization...");
            LevelPlay.OnInitSuccess += OnSdkInitSuccess;
            LevelPlay.OnInitFailed += OnSdkInitFailed;
            try
            {
                LevelPlay.Init(AppKey);
            }
            catch (Exception e)
            {
                Debug.LogError($"[AdsService] Exception during retry Init: {e}");
            }
        }
    }

    private void OnSdkInitSuccess(LevelPlayConfiguration configuration)
    {
        IsInitialized = true;
        Debug.Log($"[AdsService] SDK Initialization Succeeded. Configuration: {configuration}");
        LogAdsState("Init success");
        
        // Small delay to ensure SDK is fully ready before creating ads
        DelayedAdCreationAsync().Forget();
    }
    
    private async UniTask DelayedAdCreationAsync()
    {
        var token = cts.Token;
        // Wait a brief moment for SDK to be fully ready
        await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: token);
        
        Debug.Log("[AdsService] Creating banner and interstitial ad objects after init.");
        CreateBannerAd();
        CreateInterstitialAd();
    }

    #endregion

    #region Interstitial Ads

    private void CreateInterstitialAd()
    {
        if (!IsInitialized)
        {
            Debug.LogWarning("[AdsService] Skipping interstitial creation because SDK is not initialized.");
            return;
        }
        
        CleanupInterstitialAd();
        
        try
        {
            Debug.Log($"[AdsService] Creating interstitial ad with unit ID: {InterstitialAdId}");
            interstitialAd = new LevelPlayInterstitialAd(InterstitialAdId);

            interstitialAd.OnAdLoaded += OnInterstitialLoaded;
            interstitialAd.OnAdClosed += OnInterstitialClosed;
            interstitialAd.OnAdDisplayed += OnInterstitialDisplayed;
            interstitialAd.OnAdClicked += OnInterstitialClicked;
            interstitialAd.OnAdLoadFailed += OnInterstitialLoadFailed;
            interstitialAd.OnAdDisplayFailed += OnInterstitialDisplayFailed;

            LoadInterstitialAd();
        }
        catch (Exception e)
        {
            Debug.LogError($"[AdsService] Exception while creating interstitial ad: {e}");
        }
    }

    private void LoadInterstitialAd()
    {
        if (interstitialAd == null)
        {
            Debug.LogWarning("[AdsService] Interstitial load requested before ad object existed. Recreating.");
            CreateInterstitialAd();
            return;
        }

        Debug.Log($"[AdsService] Requesting interstitial load for unit ID: {InterstitialAdId}");
        interstitialAd.LoadAd();
        InterstitialAdCompleted = false;
        LogAdsState("Interstitial load requested");
    }

    private void OnInterstitialLoaded(LevelPlayAdInfo adInfo)
    {
        Debug.Log($"[AdsService] Interstitial loaded. AdInfo: {adInfo}");
        LogAdsState("Interstitial loaded");
    }

    private void OnInterstitialDisplayed(LevelPlayAdInfo adInfo)
    {
        Debug.Log($"[AdsService] Interstitial displayed. AdInfo: {adInfo}");
    }

    private void OnInterstitialClosed(LevelPlayAdInfo adInfo)
    {
        InterstitialAdCompleted = true;
        Debug.Log($"[AdsService] Interstitial closed. AdInfo: {adInfo}");
        LogAdsState("Interstitial closed");
        LogEventSafe(AnalyticsEvents.AdWatched, new Dictionary<string, object>
        {
            { "ad_format", "interstitial" },
            { "placement", InterstitialPlacementName },
            { "result", "completed" }
        });
        OnInterstitialAdClosed?.Invoke();
        LoadInterstitialAd();
    }

    private void OnInterstitialClicked(LevelPlayAdInfo adInfo)
    {
        Debug.Log($"[AdsService] Interstitial clicked. AdInfo: {adInfo}");
    }

    private void OnInterstitialLoadFailed(LevelPlayAdError error)
    {
        Debug.LogError($"[AdsService] Interstitial load failed: {error}");
        LogAdsState("Interstitial load failed");
        RetryLoadInterstitialAsync(3f).Forget();
    }

    private void OnInterstitialDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        Debug.LogError($"[AdsService] Interstitial display failed: {error}");
        Debug.LogError($"[AdsService] Interstitial display failure AdInfo: {adInfo}");
        LogAdsState("Interstitial display failed");
        LogEventSafe(AnalyticsEvents.AdSkipped, new Dictionary<string, object>
        {
            { "ad_format", "interstitial" },
            { "placement", InterstitialPlacementName },
            { "reason", "display_failed" }
        });
        InterstitialAdCompleted = true;
        OnInterstitialAdClosed?.Invoke();
    }

    private async UniTask RetryLoadInterstitialAsync(float delay)
    {
        var token = cts.Token;
        Debug.Log($"[AdsService] Scheduling interstitial reload in {delay:0.0}s");
        await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: token);
        LoadInterstitialAd();
    }

    public void ShowInterstitialAd()
    {
        // In the Unity Editor we don't rely on the real network interstitial,
        // as the placeholder ad can behave differently (e.g. close not firing).
        // Instead, simulate an immediate completion so game flow can continue.
#if UNITY_EDITOR
        Debug.Log("[AdsService] Simulating interstitial ad completion in Editor.");
        InterstitialAdCompleted = true;
        LogEventSafe(AnalyticsEvents.AdWatched, new Dictionary<string, object>
        {
            { "ad_format", "interstitial" },
            { "placement", InterstitialPlacementName },
            { "result", "editor_simulated" }
        });
        OnInterstitialAdClosed?.Invoke();
        return;
#else
        if (!IsInitialized || interstitialAd == null)
        {
            Debug.LogWarning($"[AdsService] ShowInterstitialAd ignored. IsInitialized={IsInitialized}, HasInterstitial={interstitialAd != null}");
            LogEventSafe(AnalyticsEvents.AdSkipped, new Dictionary<string, object>
            {
                { "ad_format", "interstitial" },
                { "placement", InterstitialPlacementName },
                { "reason", "not_initialized" }
            });
            return;
        }

        HideBannerAd();

        if (interstitialAd.IsAdReady())
        {
            Debug.Log($"[AdsService] Showing interstitial with placement name '{InterstitialPlacementName}' and unit ID {InterstitialAdId}");
            interstitialAd.ShowAd(InterstitialPlacementName);
        }
        else
        {
            Debug.LogWarning("[AdsService] Interstitial show requested before ready. Triggering load instead.");
            LogEventSafe(AnalyticsEvents.AdSkipped, new Dictionary<string, object>
            {
                { "ad_format", "interstitial" },
                { "placement", InterstitialPlacementName },
                { "reason", "not_ready" }
            });
            LoadInterstitialAd();
        }
#endif
    }

    #endregion

    #region Banner Ads

    private void CreateBannerAd()
    {
        if (!IsInitialized)
        {
            Debug.LogWarning("[AdsService] Skipping banner creation because SDK is not initialized.");
            return;
        }
        
        CleanupBannerAd();
        Debug.Log($"[AdsService] Creating banner ad with unit ID: {BannerAdId} and placement name '{BannerPlacementName}'");

        var configBuilder = new LevelPlayBannerAd.Config.Builder()
            .SetSize(LevelPlayAdSize.BANNER)
            .SetPosition(LevelPlayBannerPosition.BottomCenter)
            .SetDisplayOnLoad(false)
            .SetRespectSafeArea(false)
            .SetPlacementName(BannerPlacementName);

        bannerAd = new LevelPlayBannerAd(BannerAdId, configBuilder.Build());

        bannerAd.OnAdLoaded += OnBannerLoaded;
        bannerAd.OnAdLoadFailed += OnBannerLoadFailed;
        bannerAd.OnAdDisplayFailed += OnBannerDisplayFailed;
        bannerAd.OnAdClicked += OnBannerClicked;
    }

    private void OnBannerLoaded(LevelPlayAdInfo adInfo)
    {
        isBannerLoaded = true;
        bannerRetryCount = 0;
        bannerNoFillRetryCount = 0;

        Debug.Log($"[AdsService] Banner loaded. AdInfo: {adInfo}");
        LogAdsState("Banner loaded");

        // Show the banner after it's loaded
        if (bannerAd != null)
        {
            Debug.Log("[AdsService] Trying to show banner ad after successful load.");
            bannerAd.ShowAd();
        }
        else Debug.Log("[AdsService] BannerAd null");
    }

    private void OnBannerLoadFailed(LevelPlayAdError error)
    {
        isBannerLoaded = false;
        Debug.LogError($"[AdsService] Banner load failed: {error}");
        LogAdsState("Banner load failed");

        string errorString = error.ToString();

        // Check for Error 626 - Invalid ad unit id
        if (errorString.Contains("626") || errorString.Contains("Invalid ad unit id"))
        {
            Debug.LogError($"[AdsService] CRITICAL: Invalid ad unit ID error (626)!");
            Debug.LogError($"[AdsService] Placement ID used: {BannerAdId}");
            Debug.LogError($"[AdsService] Bundle ID: {Application.identifier}");
            Debug.LogError($"[AdsService] Platform: {Application.platform}");
            Debug.LogError($"[AdsService] Please verify in LevelPlay dashboard:");
            Debug.LogError($"[AdsService]   1. Placement '{BannerAdId}' exists and is ACTIVE");
            Debug.LogError($"[AdsService]   2. Placement is configured for BANNER ad type (not interstitial)");
            Debug.LogError($"[AdsService]   3. Placement is linked to app with bundle ID: {Application.identifier}");
            Debug.LogError($"[AdsService]   4. Placement is enabled for ANDROID platform");
            Debug.LogError($"[AdsService]   5. App status is Published/Active in LevelPlay");
            // Don't retry for invalid ad unit ID - it won't work
            bannerRetryCount = 0;
            return;
        }

        // Check if it's a "No fill" error (error code 509)
        // No fill = no ad inventory at this moment; retry a few times with delay
        if (errorString.Contains("509") || errorString.Contains("No fill") || errorString.Contains("no fill"))
        {
            bannerRetryCount = 0;
            bannerNoFillRetryCount++;
            if (bannerNoFillRetryCount <= MaxBannerNoFillRetries)
            {
                RetryLoadBannerAsync(NoFillRetryDelaySeconds).Forget();
            }
            else
            {
                bannerNoFillRetryCount = 0;
            }
            return;
        }

        // For other errors (network issues, etc.), retry with exponential backoff
        bannerRetryCount++;

        if (bannerRetryCount <= MaxBannerRetries)
        {
            Debug.LogError($"[AdsService] Banner load failed: {error}. Retry attempt {bannerRetryCount}/{MaxBannerRetries}");
            float delay = 3f * bannerRetryCount; // Exponential backoff: 3s, 6s, 9s
            RetryLoadBannerAsync(delay).Forget();
        }
        else
        {
            Debug.LogError($"[AdsService] Banner load failed after {MaxBannerRetries} retries: {error}. Stopping auto-retry.");
            bannerRetryCount = 0;
        }
    }

    private void OnBannerDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        Debug.LogError($"[AdsService] Banner display failed: {error}");
        Debug.LogError($"[AdsService] Banner display failure AdInfo: {adInfo}");
        LogAdsState("Banner display failed");
        isBannerLoaded = false;
    }

    private void OnBannerClicked(LevelPlayAdInfo adInfo)
    {
        Debug.Log($"[AdsService] Banner clicked. AdInfo: {adInfo}");
    }

    public void ShowBannerAd()
    {
        if (!IsInitialized)
        {
            Debug.LogWarning("[AdsService] ShowBannerAd ignored because SDK is not initialized.");
            return;
        }

        // Reset retry counts when ShowBannerAd is called (user action or natural retry)
        bannerRetryCount = 0;
        bannerNoFillRetryCount = 0;
        Debug.Log($"[AdsService] ShowBannerAd requested. HasBanner={bannerAd != null}, IsBannerLoaded={isBannerLoaded}");

        if (bannerAd == null)
        {
            CreateBannerAd();
        }

        if (isBannerLoaded && bannerAd != null)
        {
            bannerAd.ShowAd();
        }
        else
        {
            Debug.Log("[AdsService] Banner not ready yet. Loading banner instead of showing.");
            if (bannerAd != null)
            {
                Debug.Log($"[AdsService] Calling LoadAd() on banner with unit ID: {BannerAdId}");
                bannerAd.LoadAd();
            }
            else
            {
                Debug.LogError("[AdsService] BannerAd is null, cannot load");
            }
        }
    }

    private async UniTask RetryLoadBannerAsync(float delay)
    {
        var token = cts.Token;
        Debug.Log($"[AdsService] Scheduling banner reload in {delay:0.0}s");
        await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: token);
        if (bannerAd != null && IsInitialized && !isBannerLoaded)
        {
            Debug.Log($"[AdsService] Retrying banner load for unit ID: {BannerAdId}");
            bannerAd.LoadAd();
        }
    }

    public void HideBannerAd()
    {
        if (!IsInitialized || bannerAd == null)
        {
            Debug.Log($"[AdsService] HideBannerAd ignored. IsInitialized={IsInitialized}, HasBanner={bannerAd != null}");
            return;
        }

        Debug.Log("[AdsService] Hiding banner ad.");
        bannerAd.HideAd();
    }

    #endregion

    #region Cleanup & Utilities

    private void CleanupInterstitialAd()
    {
        if (interstitialAd == null)
            return;

        interstitialAd.OnAdLoaded -= OnInterstitialLoaded;
        interstitialAd.OnAdDisplayed -= OnInterstitialDisplayed;
        interstitialAd.OnAdClosed -= OnInterstitialClosed;
        interstitialAd.OnAdClicked -= OnInterstitialClicked;
        interstitialAd.OnAdLoadFailed -= OnInterstitialLoadFailed;

        interstitialAd = null;
    }

    private void CleanupBannerAd()
    {
        if (bannerAd == null)
            return;

        bannerAd.OnAdLoaded -= OnBannerLoaded;
        bannerAd.OnAdLoadFailed -= OnBannerLoadFailed;
        bannerAd.OnAdDisplayFailed -= OnBannerDisplayFailed;
        bannerAd.OnAdClicked -= OnBannerClicked;
        bannerAd = null;
    }

    private void LogEventSafe(string eventName, Dictionary<string, object> parameters = null)
    {
        try
        {
            analyticsService?.LogEvent(eventName, parameters);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AdsService] Failed to log event {eventName}: {e.Message}");
        }
    }

    private void LogAdsState(string context)
    {
        Debug.Log(
            $"[AdsService] State Snapshot ({context}) | " +
            $"Initialized={IsInitialized}, " +
            $"Internet={Application.internetReachability}, " +
            $"BundleId={Application.identifier}, " +
            $"BannerUnit={BannerAdId}, " +
            $"BannerLoaded={isBannerLoaded}, " +
            $"HasBanner={bannerAd != null}, " +
            $"InterstitialUnit={InterstitialAdId}, " +
            $"InterstitialReady={InterstitialAdReady}, " +
            $"InterstitialCompleted={InterstitialAdCompleted}, " +
            $"HasInterstitial={interstitialAd != null}");
    }

    public void Dispose()
    {
        Debug.Log("[AdsService] Disposing ads service.");
        CleanupInterstitialAd();
        CleanupBannerAd();

        LevelPlay.OnInitSuccess -= OnSdkInitSuccess;
        LevelPlay.OnInitFailed -= OnSdkInitFailed;

        if (!cts.IsCancellationRequested)
        {
            cts.Cancel();
        }
        cts.Dispose();
    }

    public void ForceMarkAdComplete()
    {
        InterstitialAdCompleted = true;
    }

    #endregion
}
