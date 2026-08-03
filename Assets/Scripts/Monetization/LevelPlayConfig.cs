using System;

[Serializable]
public class LevelPlayConfig
{
    public string appKey;
    public string bannerAdId;
    public string interstitialAdId;

    public bool IsValid =>
        !string.IsNullOrWhiteSpace(appKey) &&
        !string.IsNullOrWhiteSpace(bannerAdId) &&
        !string.IsNullOrWhiteSpace(interstitialAdId);
}
