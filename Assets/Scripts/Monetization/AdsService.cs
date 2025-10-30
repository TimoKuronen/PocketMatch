using System;
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

    public event Action OnInterstitialAdClosed;
    public bool InterstitialAdCompleted { get; private set; } = false;

    #region Initialization

    [Inject]
    public void Contstruct()
    {
        Debug.Log("[AdsManager] Initializing LevelPlay SDK...");

        // Always unsubscribe before subscribing again
        LevelPlay.OnInitSuccess -= OnSdkInitSuccess;
        LevelPlay.OnInitFailed -= OnSdkInitFailed;

        LevelPlay.OnInitSuccess += OnSdkInitSuccess;
        LevelPlay.OnInitFailed += OnSdkInitFailed;

        LevelPlay.ValidateIntegration();
        LevelPlay.Init(AppKey);
    }

    private void OnSdkInitFailed(LevelPlayInitError error)
    {
        Debug.LogError($"[AdsManager] SDK Initialization Failed: {error}");
    }

    private void OnSdkInitSuccess(LevelPlayConfiguration configuration)
    {
        Debug.Log("[AdsManager] SDK Initialization Completed Successfully");

        CreateBannerAd();
        CreateInterstitialAd();
    }

    #endregion

    #region Interstitial Ads

    private void CreateInterstitialAd()
    {
        Debug.Log("[AdsManager] Creating Interstitial Ad...");

        CleanupInterstitialAd(); // Prevent duplicate subscriptions
        interstitialAd = new LevelPlayInterstitialAd(InterstitialAdId);

        interstitialAd.OnAdLoaded += OnInterstitialLoaded;
        interstitialAd.OnAdDisplayed += OnInterstitialDisplayed;
        interstitialAd.OnAdClosed += OnInterstitialClosed;
        interstitialAd.OnAdClicked += OnInterstitialClicked;

        LoadInterstitialAd();
    }

    private void LoadInterstitialAd()
    {
        if (interstitialAd == null)
        {
            Debug.LogWarning("[AdsManager] InterstitialAd is null, recreating...");
            CreateInterstitialAd();
            return;
        }

        Debug.Log("[AdsManager] Loading Interstitial Ad...");
        interstitialAd.LoadAd();
        InterstitialAdCompleted = false;
    }

    private void OnInterstitialLoaded(LevelPlayAdInfo adInfo)
    {
        Debug.Log("[AdsManager] Interstitial Ad Loaded");
    }

    private void OnInterstitialDisplayed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("[AdsManager] Interstitial Ad Displayed");
        LogEventSafe("interstitial_ad_started");
    }

    private void OnInterstitialClosed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("[AdsManager] Interstitial Ad Closed");

        InterstitialAdCompleted = true;
        LogEventSafe("interstitial_ad_completed");

        OnInterstitialAdClosed?.Invoke();

        // Reload for next round
        LoadInterstitialAd();
    }

    private void OnInterstitialClicked(LevelPlayAdInfo adInfo)
    {
        Debug.Log("[AdsManager] Interstitial Ad Clicked");
        LogEventSafe("interstitial_ad_clicked");
    }

    private void OnInterstitialLoadFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        Debug.LogWarning($"[AdsManager] Interstitial load failed: {error}");
        CoroutineMonoBehavior.Instance.StartCoroutine(RetryLoadInterstitial(3f));
    }

    private System.Collections.IEnumerator RetryLoadInterstitial(float delay)
    {
        yield return new WaitForSeconds(delay);
        LoadInterstitialAd();
    }

    public void ShowInterstitialAd()
    {
        HideBannerAd();

        if (interstitialAd == null)
        {
            Debug.LogWarning("[AdsManager] Interstitial Ad is not created yet.");
            return;
        }

        if (interstitialAd.IsAdReady())
        {
            Debug.Log("[AdsManager] Showing Interstitial Ad...");
            interstitialAd.ShowAd();
        }
        else
        {
            Debug.Log("[AdsManager] Interstitial not ready, reloading...");
            LoadInterstitialAd();
        }
    }

    #endregion

    #region Banner Ads

    private void CreateBannerAd()
    {
        Debug.Log("[AdsManager] Creating Banner Ad...");

        CleanupBannerAd();

        var configBuilder = new LevelPlayBannerAd.Config.Builder()
            .SetSize(LevelPlayAdSize.BANNER)
            .SetPosition(LevelPlayBannerPosition.BottomCenter)
            .SetDisplayOnLoad(true)
            .SetRespectSafeArea(true)
            .SetPlacementName("bannerPlacement");

        bannerAd = new LevelPlayBannerAd(BannerAdId, configBuilder.Build());

        bannerAd.OnAdClicked += OnBannerClicked;
    }

    private void OnBannerClicked(LevelPlayAdInfo adInfo)
    {
        Debug.Log("[AdsManager] Banner Ad Clicked");
        LogEventSafe("banner_ad_clicked");
    }

    public void ShowBannerAd()
    {
        if (bannerAd == null)
        {
            Debug.LogError("[AdsManager] Banner Ad is not created yet.");
            return;
        }

        Debug.Log("[AdsManager] Loading Banner Ad...");
        bannerAd.LoadAd();
    }

    public void HideBannerAd()
    {
        Debug.Log("[AdsManager] Hiding Banner Ad...");
        bannerAd?.HideAd();
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

        interstitialAd = null;
    }

    private void CleanupBannerAd()
    {
        if (bannerAd == null)
            return;

        bannerAd.OnAdClicked -= OnBannerClicked;
        bannerAd = null;
    }
    private void LogEventSafe(string eventName)
    {
        try
        {
            //var analytics = Services.Get<IAnalyticsManager>();
            //analytics?.LogEvent(eventName);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AdsManager] Failed to log event {eventName}: {e.Message}");
        }
    }

    public void Dispose()
    {
        CleanupInterstitialAd();
        CleanupBannerAd();

        LevelPlay.OnInitSuccess -= OnSdkInitSuccess;
        LevelPlay.OnInitFailed -= OnSdkInitFailed;

        Debug.Log("[AdsManager] Disposed.");
    }

#if UNITY_EDITOR
    public void ForceMarkAdComplete()
    {
        InterstitialAdCompleted = true;
    }
#endif

    #endregion
}
