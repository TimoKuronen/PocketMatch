using System;
using UnityEngine;

/// <summary>
/// Debug-only ad switches: skip the interstitial gate in scene transitions, and simulate ad outcomes on
/// device so the game-side flows can be verified independently of network fill. Both are always off in
/// release builds. Manual "Show Interstitial Ad" still works.
/// </summary>
public static class AdsDebugConfig
{
    private const string SkipPrefKey = "AdsDebugSkipInterstitials";
    private const string SimulatePrefKey = "AdsDebugSimulateAds";
    private const string RewardedResultPrefKey = "AdsDebugSimulatedRewardedResult";

    /// <summary>Raised when a simulated banner should appear or disappear.</summary>
    public static event Action<bool> SimulatedBannerChanged;

    public static bool SkipInterstitials
    {
        get => DebugTools.IsEnabled && PlayerPrefs.GetInt(SkipPrefKey, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(SkipPrefKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static bool SimulateAds
    {
        get => DebugTools.IsEnabled && PlayerPrefs.GetInt(SimulatePrefKey, 0) == 1;
        set
        {
            PlayerPrefs.SetInt(SimulatePrefKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    /// <summary>True in the Editor, or on device when simulation is switched on.</summary>
    public static bool IsSimulating => Application.isEditor || SimulateAds;

    public static RewardedAdResult SimulatedRewardedResult
    {
        get => (RewardedAdResult)Mathf.Clamp(PlayerPrefs.GetInt(RewardedResultPrefKey, 0), 0, 2);
        set
        {
            PlayerPrefs.SetInt(RewardedResultPrefKey, (int)value);
            PlayerPrefs.Save();
        }
    }

    public static void CycleSimulatedRewardedResult() =>
        SimulatedRewardedResult = (RewardedAdResult)(((int)SimulatedRewardedResult + 1) % 3);

    public static void RaiseSimulatedBanner(bool visible) => SimulatedBannerChanged?.Invoke(visible);
}
