using System;
using System.Collections;
using Unity.Services.LevelPlay;
using UnityEngine;
using VContainer;

public class AdsService : IAdsService, IDisposable
{
    private const string AppKey = "23b074f85";
    private const string BannerAdId = "dq0x680o73ez3iyt";
    private const string InterstitialAdId = "adgl52j6cwsc0pge";

    private LevelPlayBannerAd bannerAd;
    private LevelPlayInterstitialAd interstitialAd;
    private IAnalyticsService analyticsService;
    private bool isBannerLoaded = false;
    private int bannerRetryCount = 0;
    private const int MaxBannerRetries = 3;

    public event Action OnInterstitialAdClosed;

    public bool IsInitialized { get; private set; } = false;
    public bool InterstitialAdReady => interstitialAd?.IsAdReady() ?? false;
    public bool InterstitialAdCompleted { get; private set; } = false;

    #region Initialization

    [Inject]
    public void Construct(IAnalyticsService analyticsService)
    {
        this.analyticsService = analyticsService;
        CoroutineMonoBehavior.Instance.StartCoroutine(DelayedInit());
    }

    private IEnumerator DelayedInit()
    {
        // Wait a bit longer to ensure Unity Services are ready
        yield return new WaitForSeconds(0.5f);
        
        // Check network connectivity before initializing
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            Debug.LogWarning("[AdsService] No internet connection. Retrying initialization in 3 seconds...");
            yield return new WaitForSeconds(3f);
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                Debug.LogError("[AdsService] Still no internet connection. Cannot initialize ads.");
                yield break;
            }
        }

        Debug.Log($"[AdsService] Initializing LevelPlay SDK with AppKey: {AppKey} on platform: {Application.platform}");
        Debug.Log($"[AdsService] Bundle ID: {Application.identifier}");
        Debug.Log($"[AdsService] App Version: {Application.version}");
        
        // Verify bundle ID matches what's configured in LevelPlay dashboard
        string expectedBundleId = "com.TimoKuronen.PocketMatch";
        if (Application.identifier != expectedBundleId)
        {
            Debug.LogWarning($"[AdsService] Bundle ID mismatch! Expected: {expectedBundleId}, Got: {Application.identifier}");
            Debug.LogWarning("[AdsService] Make sure your LevelPlay dashboard is configured with the correct bundle ID!");
        }
        
        LevelPlay.ValidateIntegration();
        LevelPlay.OnInitSuccess += OnSdkInitSuccess;
        LevelPlay.OnInitFailed += OnSdkInitFailed;
        
        try
        {
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
        Debug.LogError($"[AdsService] Error Details: {error.ToString()}");
        Debug.LogError($"[AdsService] AppKey used: {AppKey}");
        Debug.LogError($"[AdsService] Platform: {Application.platform}");
        Debug.LogError($"[AdsService] Internet Reachability: {Application.internetReachability}");
        
        // Error 2110 (Bad Request - 400) usually means invalid AppKey or configuration issue
        // Check if AppKey might be incorrect
        string errorString = error.ToString();
        if (errorString.Contains("2110") || errorString.Contains("400") || errorString.Contains("Bad Request"))
        {
            Debug.LogError("[AdsService] CRITICAL: Invalid AppKey or configuration issue detected!");
            Debug.LogError("[AdsService] Please verify:");
            Debug.LogError("  1. AppKey is correct in LevelPlay dashboard");
            Debug.LogError("  2. AppKey matches your platform (Android/iOS)");
            Debug.LogError("  3. App is properly configured in LevelPlay dashboard");
            Debug.LogError("  4. Network connectivity is working");
        }
        
        // Retry initialization after a delay (but only once to avoid spam)
        if (!IsInitialized)
        {
            CoroutineMonoBehavior.Instance.StartCoroutine(RetryInit(5f));
        }
    }
    
    private IEnumerator RetryInit(float delay)
    {
        yield return new WaitForSeconds(delay);
        
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
        CreateBannerAd();
        CreateInterstitialAd();
    }

    #endregion

    #region Interstitial Ads

    private void CreateInterstitialAd()
    {
        CleanupInterstitialAd();
        interstitialAd = new LevelPlayInterstitialAd(InterstitialAdId);

        interstitialAd.OnAdLoaded += OnInterstitialLoaded;
        interstitialAd.OnAdClosed += OnInterstitialClosed;
        interstitialAd.OnAdDisplayed += OnInterstitialDisplayed;
        interstitialAd.OnAdClicked += OnInterstitialClicked;
        interstitialAd.OnAdLoadFailed += OnInterstitialLoadFailed;
        interstitialAd.OnAdDisplayFailed += OnInterstitialDisplayFailed;

        LoadInterstitialAd();
    }

    private void LoadInterstitialAd()
    {
        if (interstitialAd == null)
        {
            CreateInterstitialAd();
            return;
        }

        interstitialAd.LoadAd();
        InterstitialAdCompleted = false;
    }

    private void OnInterstitialLoaded(LevelPlayAdInfo adInfo) { }

    private void OnInterstitialDisplayed(LevelPlayAdInfo adInfo)
    {
        LogEventSafe("interstitial_ad_started");
    }

    private void OnInterstitialClosed(LevelPlayAdInfo adInfo)
    {
        InterstitialAdCompleted = true;
        LogEventSafe("interstitial_ad_completed");
        OnInterstitialAdClosed?.Invoke();
        LoadInterstitialAd();
    }

    private void OnInterstitialClicked(LevelPlayAdInfo adInfo)
    {
        LogEventSafe("interstitial_ad_clicked");
    }

    private void OnInterstitialLoadFailed(LevelPlayAdError error)
    {
        CoroutineMonoBehavior.Instance.StartCoroutine(RetryLoadInterstitial(3f));
    }

    private void OnInterstitialDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        Debug.LogError($"[AdsService] Interstitial display failed: {error}");
        InterstitialAdCompleted = true;
        OnInterstitialAdClosed?.Invoke();
    }

    private IEnumerator RetryLoadInterstitial(float delay)
    {
        yield return new WaitForSeconds(delay);
        LoadInterstitialAd();
    }

    public void ShowInterstitialAd()
    {
        if (!IsInitialized || interstitialAd == null)
            return;

        HideBannerAd();

        if (interstitialAd.IsAdReady())
        {
            interstitialAd.ShowAd();
        }
        else
        {
            LoadInterstitialAd();
        }
    }

    #endregion

    #region Banner Ads

    private void CreateBannerAd()
    {
        CleanupBannerAd();

        var configBuilder = new LevelPlayBannerAd.Config.Builder()
            .SetSize(LevelPlayAdSize.BANNER)
            .SetPosition(LevelPlayBannerPosition.BottomCenter)
            .SetDisplayOnLoad(false)
            .SetRespectSafeArea(false)
            .SetPlacementName("bannerPlacement");

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

        Debug.Log("[AdsService] BannerAd loaded");

        // Show the banner after it's loaded
        if (bannerAd != null)
        {
            Debug.Log("[AdsService] trying to show banner ad");
            bannerAd.ShowAd();
        }
        else Debug.Log("[AdsService] BannerAd null");
    }

    private void OnBannerLoadFailed(LevelPlayAdError error)
    {
        isBannerLoaded = false;

        string errorString = error.ToString();

        // Check if it's a "No fill" error (error code 509)
        // No fill means no ad is available - don't spam retries
        if (errorString.Contains("509") || errorString.Contains("No fill") || errorString.Contains("no fill"))
        {
            Debug.LogWarning($"[AdsService] Banner load failed (No fill): {error}. Will retry when ShowBannerAd is called again.");
            bannerRetryCount = 0; // Reset retry count for natural retries
            return;
        }

        // For other errors (network issues, etc.), retry with exponential backoff
        bannerRetryCount++;

        if (bannerRetryCount <= MaxBannerRetries)
        {
            Debug.LogError($"[AdsService] Banner load failed: {error}. Retry attempt {bannerRetryCount}/{MaxBannerRetries}");
            float delay = 3f * bannerRetryCount; // Exponential backoff: 3s, 6s, 9s
            CoroutineMonoBehavior.Instance.StartCoroutine(RetryLoadBanner(delay));
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
        isBannerLoaded = false;
    }

    private void OnBannerClicked(LevelPlayAdInfo adInfo)
    {
        LogEventSafe("banner_ad_clicked");
    }

    public void ShowBannerAd()
    {
        if (!IsInitialized)
            return;

        // Reset retry count when ShowBannerAd is called (user action or natural retry)
        bannerRetryCount = 0;

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
            Debug.Log("[AdsService] loading instead");
            if (bannerAd != null)
            {
                Debug.Log("[AdsService] Calling LoadAd() on banner");
                bannerAd.LoadAd();
            }
            else
            {
                Debug.LogError("[AdsService] BannerAd is null, cannot load");
            }
        }
    }

    private IEnumerator RetryLoadBanner(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (bannerAd != null && IsInitialized)
        {
            Debug.Log("[AdsService] Retrying banner load");
            bannerAd.LoadAd();
        }
    }

    public void HideBannerAd()
    {
        if (!IsInitialized || bannerAd == null)
            return;

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

    private void LogEventSafe(string eventName)
    {
        try
        {
            analyticsService?.LogEvent(eventName);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AdsService] Failed to log event {eventName}: {e.Message}");
        }
    }

    public void Dispose()
    {
        CleanupInterstitialAd();
        CleanupBannerAd();

        LevelPlay.OnInitSuccess -= OnSdkInitSuccess;
        LevelPlay.OnInitFailed -= OnSdkInitFailed;
    }

    public void ForceMarkAdComplete()
    {
        InterstitialAdCompleted = true;
    }

    #endregion
}
