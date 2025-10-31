using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;

public static class Loader
{
    public enum GameScene
    {
        Empty,
        MainMenu,
        Loader,
        PlayScene
    }

    public static event Action OnSceneLoadStarted;

    private static AsyncOperation loadingAsyncOperation;
    public static bool isLoading;
    private static GameScene? targetScene;
    private static float delayBeforeLoading = 0f;

    private static string previousSceneName;

#if UNITY_EDITOR
    private static string editorStartingScene => EditorBootstrapper.CurrentSceneName;
#endif

    #region Public

    public static void Load(GameScene nextScene, float delay = 0f)
    {
        if (isLoading)
        {
            // Debug.LogWarning("[Loader] Load requested while another load is active.");
            return;
        }

        previousSceneName = SceneManager.GetActiveScene().name;

        targetScene = nextScene;
        delayBeforeLoading = delay;

        SceneManager.LoadScene(GameScene.Loader.ToString(), LoadSceneMode.Single);
    }

    internal static IEnumerator ContinueLoadFromLoader()
    {
        // Debug.Log("[Loader] Continuing load from Loader scene...");

        isLoading = false;
        delayBeforeLoading = 0f;
        previousSceneName = SceneManager.GetActiveScene().name;

#if UNITY_EDITOR
        // Handle editor case first — figure out which scene Play started from
        if (!targetScene.HasValue)
        {
            if (Enum.TryParse(editorStartingScene, out GameScene fromEditor))
            {
                if (fromEditor == GameScene.Loader)
                {
                    targetScene = GameScene.MainMenu;
                }
                else
                    targetScene = fromEditor;

                // Debug.Log($"[Loader] Editor play started from '{editorStartingScene}', reloading same scene: {targetScene}");
            }
            else
            {
                // Debug.LogWarning($"[Loader] Unknown editor start scene '{editorStartingScene}', defaulting to MainMenu.");
                targetScene = GameScene.MainMenu;
            }
        }
#else
        // Runtime fallback
        if (!targetScene.HasValue)
        {
            // Debug.Log("[Loader] No target specified, defaulting to MainMenu.");
            targetScene = GameScene.MainMenu;
        }
#endif

        yield return LoadSceneAsync(targetScene.Value, delayBeforeLoading);
    }

    public static void Restart()
    {
        GameScene current = GetCurrentScene();
        Load(current, 0.1f);
    }

    public static IEnumerator ShowInterstitialThenContinue(IAdsService adsService, GameScene sceneToLoad)
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
            // Debug.LogWarning("[Loader] Interstitial failed or timed out. Continuing.");
            adsService.ForceMarkAdComplete();
        }

        Load(sceneToLoad);
    }

    #endregion

    #region Core

    private static IEnumerator LoadSceneAsync(GameScene targetScene, float delay)
    {
        // Debug.Log($"[Loader] Loader scene still loaded? {SceneManager.GetSceneByName(GameScene.Loader.ToString()).isLoaded}");
        string targetName = targetScene.ToString();
        // Debug.Log($"[Loader] Starting additive load of scene '{targetName}' (delay {delay:0.00}s)");

        isLoading = true;
        OnSceneLoadStarted?.Invoke();

        try
        {
            // Wait until the Loader scene is fully active
            while (SceneManager.GetActiveScene().name != GameScene.Loader.ToString())
                yield return null;

            // Optional delay (e.g. short ad fade-in/out)
            if (delay > 0f)
            {
                yield return new WaitForSecondsRealtime(delay);
                // Debug.Log("[Loader] Delay completed.");
            }

            // Sanity check – make sure the target exists in Build Settings
            if (!Application.CanStreamedLevelBeLoaded(targetName))
            {
                // Debug.LogError($"[Loader] Scene '{targetName}' not found in Build Settings!");
                yield break;
            }

            // Unload any previous instance of the same scene (in case of restart)
            Scene existing = SceneManager.GetSceneByName(targetName);
            if (existing.IsValid() && existing.isLoaded)
            {
                // Debug.Log($"[Loader] Unloading existing instance of '{targetName}'...");
                yield return SceneManager.UnloadSceneAsync(existing);
            }

            // Debug.Log($"[Loader] Beginning async additive load of '{targetName}'...");
            loadingAsyncOperation = SceneManager.LoadSceneAsync(targetName, LoadSceneMode.Additive);
            loadingAsyncOperation.allowSceneActivation = true;

            // Wait until the scene is fully loaded
            while (!loadingAsyncOperation.isDone)
            {
                // Debug.Log($"[Loader] Loading {targetName}: {loadingAsyncOperation.progress * 100f:0}%");
                yield return null;
            }

            // Debug.Log($"[Loader] Async load of '{targetName}' completed.");

            // Activate the newly loaded scene
            Scene loadedScene = SceneManager.GetSceneByName(targetName);
            if (loadedScene.IsValid() && loadedScene.isLoaded)
            {
                SceneManager.SetActiveScene(loadedScene);
                // Debug.Log($"[Loader] Active scene set to '{targetName}'.");
            }
            else
            {
                // Debug.LogError($"[Loader] Failed to set active scene '{targetName}'.");
            }

            // Unload the Loader scene AFTER we’ve switched
            // Debug.Log("[Loader] Unloading Loader scene...");
            yield return SceneManager.UnloadSceneAsync(GameScene.Loader.ToString());
            // Debug.Log("[Loader] Loader scene unloaded successfully.");
        }
        finally
        {
            loadingAsyncOperation = null;
            isLoading = false;
            // Debug.Log($"[Loader] Scene '{targetScene}' load process finished (isLoading reset).");
        }
    }


    #endregion

    #region Utility

    public static GameScene GetCurrentScene()
    {
        if (Enum.TryParse(SceneManager.GetActiveScene().name, out GameScene result))
            return result;
        return default;
    }

    public static float GetLoadingProgress() =>
        loadingAsyncOperation?.progress ?? 1f;

    public static bool IsGameScene() =>
        GetCurrentScene() != GameScene.Empty && GetCurrentScene() > GameScene.Loader;

    #endregion

    private class LoaderHost : MonoBehaviour { }

    private static LoaderHost host;

    private static MonoBehaviour EnsureHost()
    {
        if (host == null)
        {
            var go = new GameObject("[LoaderHost]");         
            UnityEngine.Object.DontDestroyOnLoad(go);
            host = go.AddComponent<LoaderHost>();
        }
        return host;
    }

    private static void Run(IEnumerator routine)
    {
        EnsureHost().StartCoroutine(routine);
    }

    public static void Load(GameScene scene)
    {
        if (isLoading) 
            return;

        previousSceneName = SceneManager.GetActiveScene().name;
        targetScene = scene;

        GameSignals.ResetSessionLoaded();

        SceneManager.LoadScene(GameScene.Loader.ToString(), LoadSceneMode.Single);
    }

    internal static void ContinueFromLoader()
    {
        Run(ContinueLoadFromLoader());
    }
}
