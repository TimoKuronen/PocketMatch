using UnityEngine;

/// <summary>
/// Global toggle for board debug logging, backed by PlayerPrefs and a simple static API.
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

