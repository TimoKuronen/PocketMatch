using System;
using System.Threading;
using Cysharp.Threading.Tasks;

public enum RewardedAdResult
{
    Rewarded,
    Dismissed,
    Unavailable
}

public interface IAdsService
{
    void ShowInterstitialAd();
    void ShowBannerAd();
    void HideBannerAd();
    void ForceMarkAdComplete();

    /// <summary>
    /// Shows a rewarded ad and resolves once the outcome is known. Never throws for ad failures;
    /// not-ready, no-fill, and display failures resolve as <see cref="RewardedAdResult.Unavailable"/>.
    /// </summary>
    UniTask<RewardedAdResult> ShowRewardedAdAsync(CancellationToken cancellationToken = default);

    bool IsInitialized { get; }
    bool InterstitialAdReady { get; }
    bool InterstitialAdCompleted { get; }

    /// <summary>True when a rewarded ad can be requested (SDK up and rewarded unit configured, or Editor simulation).</summary>
    bool IsRewardedAdAvailable { get; }

    event Action OnInterstitialAdClosed;
}
