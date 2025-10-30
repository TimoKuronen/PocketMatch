using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;

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

    public static IEnumerator CallDelayedLoad(Scene scene, float delay = 0.0f)
    {
        OnSceneLoadStarted?.Invoke();

        if(delay > 0)
            yield return new WaitForSecondsRealtime(delay);

        targetScene = scene;
        //Debug.Log($"[Loader] Loading scene via Loader: {scene}");
        SceneManager.LoadScene(Scene.Loader.ToString());
    }

    public static IEnumerator ShowInterstitialThenContinue(IAdsService adsService, Scene sceneToLoad)
    {
        adsService.ShowInterstitialAd();

        float timeout = 3f;
        float timer = 0f;

        while (!adsService.InterstitialAdCompleted && timer < timeout)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!adsService.InterstitialAdCompleted)
        {
            Debug.LogWarning("Interstitial failed or no fill. Continuing flow.");
            adsService.ForceMarkAdComplete();
        }
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
        CoroutineMonoBehavior.RunStatic(CallDelayedLoad(currentScene, 0.1f));
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

        if (delay > 0)
            yield return new WaitForSecondsRealtime(delay);

        var bootstrap = LifetimeScope.Find<BootstrapLifetimeScope>(); // finds the DDOL bootstrap

        using (LifetimeScope.EnqueueParent(bootstrap))
        {
            loadingAsyncOperation = SceneManager.LoadSceneAsync(scene.ToString(), LoadSceneMode.Additive);
            while (!loadingAsyncOperation.isDone)
                yield return null;
        }

        var loadedScene = SceneManager.GetSceneByName(scene.ToString());
        SceneManager.SetActiveScene(loadedScene);

        SceneManager.UnloadSceneAsync(Scene.Loader.ToString());

        loadingAsyncOperation = null;
        targetScene = null;
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