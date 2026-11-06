using System;

[Serializable]
public class LevelPlayConfig
{
    public string appKey;
    public string bannerAdId;
    public string interstitialAdId;
    public string rewardedAdId;

    public bool IsValid =>
        !string.IsNullOrWhiteSpace(appKey) &&
        !string.IsNullOrWhiteSpace(bannerAdId) &&
        !string.IsNullOrWhiteSpace(interstitialAdId);

    /// <summary>Rewarded is optional so configs without it keep banner and interstitial working.</summary>
    public bool HasRewardedAdId => !string.IsNullOrWhiteSpace(rewardedAdId);
}
