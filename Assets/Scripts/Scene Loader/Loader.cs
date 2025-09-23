using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Loader
{
    public enum Scene
    {
        Empty,
        MainMenu,
        Loader,
        PlayScene
    }
    public static event Action OnSceneLoadStarted;

    private static AsyncOperation loadingAsyncOperation;
    private static Scene? targetScene = null;
    private static float delayBeforeLoading = 0f;

    public static IEnumerator CallDelayedLoad(Scene scene, float delay = 0.1f)
    {
        OnSceneLoadStarted?.Invoke();

        yield return new WaitForSecondsRealtime(delay);

        targetScene = scene;
        //Debug.Log($"[Loader] Loading scene via Loader: {scene}");
        SceneManager.LoadScene(Scene.Loader.ToString());
    }

    public static void LoaderCallback()
    {
        if (!targetScene.HasValue)
        {
#if UNITY_EDITOR

            Scene currentScene = Scene.MainMenu;
            string sceneName = EditorBootstrapper.CurrentSceneName;

            if (Enum.TryParse(sceneName, out Scene sceneEnum))
                currentScene = sceneEnum;

            if (currentScene != Scene.Loader)
            {
                targetScene = currentScene;
            }
            else
            {
                targetScene = Scene.MainMenu;
            }
#else
            targetScene = Scene.MainMenu;
#endif
        }

        GameObject go = new GameObject("LoaderRunner");
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<CoroutineMonoBehavior>()
            .StartCoroutine(LoadSceneAsync(targetScene.Value, delayBeforeLoading));
    }

    public static void Restart()
    {
        Scene currentScene = GetCurrentScene();
        CoroutineMonoBehavior.RunStatic(CallDelayedLoad(currentScene, 1));
    }

    public static Scene GetCurrentScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (Enum.TryParse(sceneName, out Scene sceneEnum))
            return sceneEnum;
        return default;
    }

    private static IEnumerator LoadSceneAsync(Scene scene, float delay)
    {
        OnSceneLoadStarted?.Invoke();

       // Debug.Log($"2. [Loader] Begin async load: {scene}");
        if (delay > 0)
            yield return new WaitForSecondsRealtime(delay);

        loadingAsyncOperation = SceneManager.LoadSceneAsync(scene.ToString());

        while (!loadingAsyncOperation.isDone)
            yield return null;

        loadingAsyncOperation = null;
        //Debug.Log($"3. [Loader] Finished loading: {targetScene}");

        targetScene = null;

        //Debug.Log("4. resetting: " + targetScene);
    }

    public static float GetLoadingProgress()
    {
        return loadingAsyncOperation?.progress ?? 1f;
    }

    public static bool IsGameScene()
    {
        return GetCurrentScene() != Scene.Empty && GetCurrentScene() > Scene.Loader;
    }
}