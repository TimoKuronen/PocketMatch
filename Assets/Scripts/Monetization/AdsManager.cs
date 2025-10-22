using System;
using Unity.Services.LevelPlay;
using UnityEngine;

public class AdsManager : IAdsManager
{
    private const string appKey = "23b074f85";
    private string bannerAdId = "dq0x680o73ez3iyt";
    private string interstitialAdId = "adgl52j6cwsc0pge";

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
        interstitialAd = new LevelPlayInterstitialAd(interstitialAdId);

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

            InterstitialAdCompleted = false;
        }
    }

    public void ShowInterstitialAd()
    {
        HideBannerAd();

        if (interstitialAd == null)
        {
            Debug.Log("Interstitial Ad is not created yet.");
            return;
        }

        if (interstitialAd.IsAdReady())
        {
            Debug.Log("Showing Interstitial Ad...");
            interstitialAd.ShowAd();
        }
        else
        {
            Debug.Log("Interstitial not ready yet, loading again...");
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
        bannerAd = new LevelPlayBannerAd(bannerAdId, bannerConfig);
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

    public void HideBannerAd()
    {
        Debug.Log("Hiding Banner Ad...");

        if (bannerAd != null)
            bannerAd.HideAd();
    }

    public void Dispose()
    {
        LevelPlay.OnInitSuccess -= SdkInitializationCompletedEvent;
        LevelPlay.OnInitFailed -= SdkInitializationFailedEvent;
    }

    public void ForceMarkAdComplete()
    {
        InterstitialAdCompleted = true;
    }
}
