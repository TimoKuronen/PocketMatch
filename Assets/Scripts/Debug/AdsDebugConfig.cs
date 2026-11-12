using UnityEngine;

/// <summary>
/// Debug-only switch that skips the interstitial gate in scene transitions. Defaults to on in
/// debug-enabled builds and is always off in release builds. Manual "Show Interstitial Ad" still works.
/// </summary>
public static class AdsDebugConfig
{
    private const string PrefKey = "AdsDebugSkipInterstitials";

    public static bool SkipInterstitials
    {
        get => DebugTools.IsEnabled && PlayerPrefs.GetInt(PrefKey, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(PrefKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
