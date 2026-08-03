using System;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Loads optional JSON configuration from StreamingAssets without requiring those files in source control.
/// </summary>
public static class LocalJsonConfig
{
    public static bool TryLoad<T>(string fileName, out T config) where T : class
    {
        config = null;

        if (!TryReadText(fileName, out string json) || string.IsNullOrWhiteSpace(json))
        {
            Debug.LogWarning($"[LocalJsonConfig] Missing {fileName}. Optional integrations that depend on it will degrade gracefully.");
            return false;
        }

        try
        {
            config = JsonUtility.FromJson<T>(json);
            return config != null;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LocalJsonConfig] Failed to parse {fileName}: {e.Message}");
            return false;
        }
    }

    private static bool TryReadText(string fileName, out string json)
    {
        json = null;
        string path = Path.Combine(Application.streamingAssetsPath, fileName);

#if UNITY_ANDROID && !UNITY_EDITOR
        using var request = UnityWebRequest.Get(path);
        UnityWebRequestAsyncOperation operation = request.SendWebRequest();
        while (!operation.isDone)
        {
        }

        if (request.result != UnityWebRequest.Result.Success)
        {
            return false;
        }

        json = request.downloadHandler.text;
        return true;
#else
        if (!File.Exists(path))
        {
            return false;
        }

        json = File.ReadAllText(path);
        return true;
#endif
    }
}
