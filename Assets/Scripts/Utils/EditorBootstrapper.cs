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
        UpdatePlayModeStartScene();
        
        // Update when entering play mode to catch the current scene
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }
    
    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            UpdatePlayModeStartScene();
        }
    }
    
    private static void UpdatePlayModeStartScene()
    {
        CurrentSceneName = SceneManager.GetActiveScene().name;
        
        // Check if current scene is in the build list
        string currentScenePath = SceneManager.GetActiveScene().path;
        bool isInBuildList = IsSceneInBuildList(currentScenePath);
        
        if (!isInBuildList && !string.IsNullOrEmpty(currentScenePath))
        {
            // Scene is not in build list - load it directly
            var currentSceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(currentScenePath);
            if (currentSceneAsset != null)
            {
                EditorSceneManager.playModeStartScene = currentSceneAsset;
                Debug.Log($"[EditorBootstrapper] Current scene '{CurrentSceneName}' is not in build list. Setting as play mode start scene.");
            }
            else
            {
                Debug.LogWarning($"[EditorBootstrapper] Could not load scene asset at '{currentScenePath}'. Falling back to Loader.");
                SetLoaderAsStartScene();
            }
        }
        else
        {
            // Scene is in build list - use Loader system
            SetLoaderAsStartScene();
        }
    }
    
    private static void SetLoaderAsStartScene()
    {
        var loaderScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(LoaderScenePath);
        if (loaderScene != null)
        {
            EditorSceneManager.playModeStartScene = loaderScene;
        }
        else
        {
            Debug.LogError($"[EditorBootstrapper] Loader scene not found at '{LoaderScenePath}'!");
        }
    }
    
    private static bool IsSceneInBuildList(string scenePath)
    {
        if (string.IsNullOrEmpty(scenePath))
            return false;
            
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.path == scenePath)
                return true;
        }
        
        return false;
    }
}
#endif