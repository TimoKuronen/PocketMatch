using System;

public interface IAdsService
{
    void ShowInterstitialAd();
    void ShowBannerAd();
    void HideBannerAd();
    void ForceMarkAdComplete();

    bool IsInitialized { get; }
    bool InterstitialAdReady { get; }
    bool InterstitialAdCompleted { get; }

    event Action OnInterstitialAdClosed;
}
