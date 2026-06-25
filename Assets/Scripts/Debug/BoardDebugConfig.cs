using UnityEngine;

/// <summary>
/// Global toggle for board debug logging, backed by PlayerPrefs.
/// Lives in runtime so the main menu toggle can read/write it.
/// Editor-only logging code subscribes via BoardDebugHooks.
/// </summary>
public static class BoardDebugConfig
{
    private const string PrefKey = "BoardDebugLoggingEnabled";

    public static bool IsEnabled
    {
        get => PlayerPrefs.GetInt(PrefKey, 0) == 1;
        set
        {
            PlayerPrefs.SetInt(PrefKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
