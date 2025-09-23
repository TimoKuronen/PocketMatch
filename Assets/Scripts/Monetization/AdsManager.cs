using System;
using Unity.Services.LevelPlay;
using UnityEngine;

public class AdsManager : IAdsManager
{
    /*
    Rewarded Ad: "rewardedVideo" (Android/iOS)
    Interstitial Ad: "interstitial" (Android/iOS)
    Banner Ad: "banner" (Android/iOS)
    */
    [SerializeField] private string appKey = "23b074f85";
    private LevelPlayBannerAd bannerAd;

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
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.B))
        {
            ShowBannerAd();
        }
#endif
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

    public void Dispose() { }
}