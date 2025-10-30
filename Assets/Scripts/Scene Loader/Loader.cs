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
    private static bool isLoading;
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
            Debug.LogWarning("[Loader] Load requested while another load is active.");
            return;
        }

        previousSceneName = SceneManager.GetActiveScene().name;

        targetScene = nextScene;
        delayBeforeLoading = delay;

        SceneManager.LoadScene(GameScene.Loader.ToString(), LoadSceneMode.Single);
    }

    internal static IEnumerator ContinueLoadFromLoader()
    {
#if UNITY_EDITOR
        // Handle editor case first — figure out which scene Play started from
        if (!targetScene.HasValue)
        {
            if (Enum.TryParse(editorStartingScene, out GameScene fromEditor))
            {
                targetScene = fromEditor;
                Debug.Log($"[Loader] Editor play started from '{editorStartingScene}', reloading same scene: {targetScene}");
            }
            else
            {
                Debug.LogWarning($"[Loader] Unknown editor start scene '{editorStartingScene}', defaulting to MainMenu.");
                targetScene = GameScene.MainMenu;
            }
        }
#else
        // Runtime fallback
        if (!targetScene.HasValue)
        {
            Debug.Log("[Loader] No target specified, defaulting to MainMenu.");
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
            Debug.LogWarning("[Loader] Interstitial failed or timed out. Continuing.");
            adsService.ForceMarkAdComplete();
        }

        Load(sceneToLoad);
    }

    #endregion

    #region Core

    private static IEnumerator LoadSceneAsync(GameScene targetScene, float delay)
    {
        isLoading = true;
        OnSceneLoadStarted?.Invoke();

        if (delay > 0)
            yield return new WaitForSecondsRealtime(delay);

        var bootstrap = LifetimeScope.Find<BootstrapLifetimeScope>();

        using (LifetimeScope.EnqueueParent(bootstrap))
        {
            loadingAsyncOperation = SceneManager.LoadSceneAsync(targetScene.ToString(), LoadSceneMode.Additive);
            while (!loadingAsyncOperation.isDone)
                yield return null;
        }

        var loadedScene = SceneManager.GetSceneByName(targetScene.ToString());
        SceneManager.SetActiveScene(loadedScene);
        SceneManager.UnloadSceneAsync(GameScene.Loader.ToString());

        loadingAsyncOperation = null;
        isLoading = false;
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
}
