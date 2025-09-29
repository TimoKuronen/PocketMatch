using System;
using Unity.Services.LevelPlay;
using Unity.VisualScripting;
using UnityEngine;

public class AdsManager : IAdsManager
{
    [SerializeField] private string appKey = "23b074f85";
    private LevelPlayBannerAd bannerAd;
    private LevelPlayInterstitialAd interstitialAd;
    public event Action OnInterstitialAdClosed;
    public bool InterstitialAdCompleted { get; private set; } = false;

    public void Initialize()
    {
        Debug.Log("Initializing LevelPlay SDK...");
        LevelPlay.ValidateIntegration();
        LevelPlay.OnInitSuccess += SdkInitializationCompletedEvent;
        LevelPlay.OnInitFailed += SdkInitializationFailedEvent;
        LevelPlay.Init(appKey);
        Loader.OnSceneLoadStarted += HandleAdsForSceneChange;
    }

    private void SdkInitializationFailedEvent(LevelPlayInitError error)
    {
        Debug.LogError($"LevelPlay SDK Initialization Failed: {error}");
    }

    private void SdkInitializationCompletedEvent(LevelPlayConfiguration configuration)
    {
        Debug.Log("LevelPlay SDK Initialization Completed Successfully");
        CreateBannerAd();
        CreateInterstitialAd();
    }

    private void CreateInterstitialAd()
    {
        Debug.Log("Creating Interstitial Ad...");
        interstitialAd = new LevelPlayInterstitialAd("interstitial");

        interstitialAd.OnAdLoaded += adInfo =>
        {
            Debug.Log("Interstitial Ad Loaded");
        };

        interstitialAd.OnAdDisplayed += adInfo =>
        {
            Debug.Log("Interstitial Ad Displayed");
        };

        interstitialAd.OnAdClosed += adInfo =>
        {
            Debug.Log("Interstitial Ad Closed");
            OnInterstitialAdClosed?.Invoke();
            InterstitialAdCompleted = true;
            LoadInterstitialAd(); // Reload the ad for next time
        };

        interstitialAd.OnAdClicked += adInfo =>
        {
            Debug.Log("Interstitial Ad Clicked");
        };

        LoadInterstitialAd();
    }

    private void LoadInterstitialAd()
    {
        if (interstitialAd != null)
        {
            Debug.Log("Loading Interstitial Ad...");
            interstitialAd.LoadAd();
        }
    }

    public void ShowInterstitialAd()
    {
        if (interstitialAd == null)
        {
            Debug.LogError("Interstitial Ad is not created yet.");
            return;
        }

        if (interstitialAd.IsAdReady())
        {
            Debug.Log("Showing Interstitial Ad...");
            interstitialAd.ShowAd();
        }
        else
        {
            Debug.LogWarning("Interstitial not ready yet, loading again...");
            LoadInterstitialAd();
        }
    }

    private void CreateBannerAd()
    {
        Debug.Log("Creating Banner Ad...");
        LevelPlayAdSize adSize = LevelPlayAdSize.BANNER;
        var configBuilder = new LevelPlayBannerAd.Config.Builder()
            .SetSize(adSize)
            .SetPosition(LevelPlayBannerPosition.BottomCenter)
            .SetDisplayOnLoad(true)
            .SetRespectSafeArea(true)
            .SetPlacementName("bannerPlacement");

        var bannerConfig = configBuilder.Build();
        bannerAd = new LevelPlayBannerAd("banner", bannerConfig);
    }

    public void ShowBannerAd()
    {
        if (bannerAd == null)
        {
            Debug.LogError("Banner Ad is not created yet.");
            return;
        }
        Debug.Log("Loading Banner Ad...");
        bannerAd.LoadAd();
    }

    private void HandleAdsForSceneChange()
    {
        Debug.Log("Hiding Banner Ad...");
        if (bannerAd != null)
            bannerAd.HideAd();
        InterstitialAdCompleted = false;
    }

    public void Dispose()
    {
        LevelPlay.OnInitSuccess -= SdkInitializationCompletedEvent;
        LevelPlay.OnInitFailed -= SdkInitializationFailedEvent;
        Loader.OnSceneLoadStarted -= HandleAdsForSceneChange;
    }
}
