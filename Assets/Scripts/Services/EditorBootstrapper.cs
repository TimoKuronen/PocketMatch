#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class EditorBootstrapper
{
    private const string LoaderScenePath = "Assets/_Project/Scenes/Loader.unity";
    public static string CurrentSceneName;

    static EditorBootstrapper()
    {
        var loaderScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(LoaderScenePath);
        if (loaderScene != null)
        {
            CurrentSceneName = SceneManager.GetActiveScene().name;
            //Debug.Log($"[EditorBootstrapper] Current scene: {CurrentSceneName}");
            EditorSceneManager.playModeStartScene = loaderScene;
            //Debug.Log($"[EditorBootstrapper] Play mode start scene set to Loader ({LoaderScenePath}).");
        }
        else
        {
            Debug.LogError($"[EditorBootstrapper] Loader scene not found at '{LoaderScenePath}'!");
        }
    }
}
#endif