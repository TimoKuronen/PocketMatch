#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class EditorBootstrapper
{
    private const string LoaderScenePath = "Assets/_Project/Scenes/Loader.unity";

    static EditorBootstrapper()
    {
        var loaderScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(LoaderScenePath);
        if (loaderScene != null)
        {
            EditorSceneManager.playModeStartScene = loaderScene;
            Debug.Log($"[EditorBootstrapper] Play mode start scene set to Loader ({LoaderScenePath}).");
        }
        else
        {
            Debug.LogError($"[EditorBootstrapper] Loader scene not found at '{LoaderScenePath}'!");
        }
    }
}
#endif