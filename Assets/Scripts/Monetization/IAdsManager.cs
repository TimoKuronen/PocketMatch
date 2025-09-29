using System;

public interface IAdsManager : IService
{
    void ShowBannerAd();
    void ShowInterstitialAd();

    bool InterstitialAdCompleted { get; }
    event Action OnInterstitialAdClosed;
}
