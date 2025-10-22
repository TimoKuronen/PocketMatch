using System;

public interface IAdsManager : IService
{
    void ShowBannerAd();
    void ShowInterstitialAd();
    void HideBannerAd();
    void ForceMarkAdComplete();

    bool InterstitialAdCompleted { get; }
    event Action OnInterstitialAdClosed;
}
