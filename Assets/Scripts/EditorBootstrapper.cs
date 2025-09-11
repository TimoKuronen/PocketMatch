#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class EditorBootstrapper
{
    static EditorBootstrapper()
    {
        var loaderScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/_Project/Scenes/Loader.unity");
        if (loaderScene != null)
        {
            EditorSceneManager.playModeStartScene = loaderScene;
            Debug.Log("[EditorBootstrapper] Play mode start scene forced to Loader.");
        }
        else
        {
            Debug.LogError("Loader scene not found at 'Assets/Scenes/Loader.unity'!");
        }
    }
}
#endif