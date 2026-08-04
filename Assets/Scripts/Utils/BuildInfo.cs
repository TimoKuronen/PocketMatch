using UnityEngine;

/// <summary>
/// Resolves app version and Android build number for UI labels.
/// Bundle version comes from Application.version; build number is read from the installed APK on device.
/// </summary>
public static class BuildInfo
{
    private static int? cachedAndroidVersionCode;

    public static int AndroidVersionCode
    {
        get
        {
            if (cachedAndroidVersionCode.HasValue)
                return cachedAndroidVersionCode.Value;

            cachedAndroidVersionCode = ResolveAndroidVersionCode();
            return cachedAndroidVersionCode.Value;
        }
    }

    public static string FormatVersionLabel()
    {
        return $"v{Application.version} ({AndroidVersionCode})";
    }

    private static int ResolveAndroidVersionCode()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var packageManager = activity.Call<AndroidJavaObject>("getPackageManager"))
            using (var packageInfo = packageManager.Call<AndroidJavaObject>("getPackageInfo", Application.identifier, 0))
            {
                return packageInfo.Get<int>("versionCode");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[BuildInfo] Failed to read Android versionCode: {e.Message}");
            return 0;
        }
#else
        return 0;
#endif
    }
}
