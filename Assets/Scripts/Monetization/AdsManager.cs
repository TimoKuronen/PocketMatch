using System;
using Unity.Services.Core;
using Unity.Services.LevelPlay;
using UnityEngine;

public class AdsManager : IAdsManager
{
    [SerializeField] private string appKey = "23b074f85";

    /*
        Rewarded Ad: "rewardedVideo" (Android/iOS)
        Interstitial Ad: "interstitial" (Android/iOS)
        Banner Ad: "banner" (Android/iOS)
    */
    LevelPlayBannerAd bannerAd;

    public void Initialize()
    {
        LevelPlay.ValidateIntegration(); // Force test ads

        LevelPlay.OnInitSuccess += SdkInitializationCompletedEvent;
        LevelPlay.OnInitFailed += SdkInitializationFailedEvent;

        LevelPlay.Init(appKey);
        CreateBannerAd();
        Debug.Log("Initializing LevelPlay SDK...");
    }

    public void Update()
    {
        Debug.Log("Press 'B' to show Banner Ad");
        if (Input.GetKeyDown(KeyCode.B))
        {
            ShowBannerAd();
        }
    }

    private void SdkInitializationFailedEvent(LevelPlayInitError error)
    {
        Debug.LogError($"LevelPlay SDK Initialization Failed: {error.ToString()}");
    }

    private void SdkInitializationCompletedEvent(LevelPlayConfiguration configuration)
    {
        Debug.Log("LevelPlay SDK Initialization Completed Successfully");

        CreateBannerAd();
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

    /// <summary>
    /// TO-DO : call this when scene changes..?
    /// </summary>
    public void HideBanner()
    {
        bannerAd.HideAd();
    }

    private void CreateBannerAd()
    {
        Debug.Log("Creating Banner Ad...");
        LevelPlayAdSize adSize = LevelPlayAdSize.CreateAdaptiveAdSize();
        int width = adSize.Width;
        int height = adSize.Height;

        var configBuilder = new LevelPlayBannerAd.Config.Builder();
        configBuilder.SetDisplayOnLoad(true);
        configBuilder.SetRespectSafeArea(true);
        configBuilder.SetPlacementName("bannerPlacement");
        configBuilder.SetBidFloor(1.0);
        var bannerConfig = configBuilder.Build();

        bannerAd = new LevelPlayBannerAd("banner", bannerConfig);
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}