using System;

public interface IAdsService
{
    void ShowBannerAd();
    void ShowInterstitialAd();
    void HideBannerAd();
    void ForceMarkAdComplete();

    bool InterstitialAdCompleted { get; }
    event Action OnInterstitialAdClosed;
}
