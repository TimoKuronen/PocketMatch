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
        yield return null;
        LevelPlay.ValidateIntegration();
        LevelPlay.OnInitSuccess += OnSdkInitSuccess;
        LevelPlay.OnInitFailed += OnSdkInitFailed;
        LevelPlay.Init(AppKey);
    }

    private void OnSdkInitFailed(LevelPlayInitError error)
    {
        Debug.LogError($"[AdsService] SDK Initialization Failed: {error}");
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
