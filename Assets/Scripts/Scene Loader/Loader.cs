using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    private static LoaderHost host;
    private static IAdsService pendingAdService;

#if UNITY_EDITOR
    private static string editorStartingScene => EditorBootstrapper.CurrentSceneName;
#endif

    #region Public API

    public static void Load(GameScene nextScene, float delay = 0f)
    {
        if (isLoading)
            return;

        targetScene = nextScene;
        GameSignals.ResetSessionLoaded();

        SceneManager.LoadScene(GameScene.Loader.ToString(), LoadSceneMode.Single);
    }

    internal static void ContinueFromLoader()
    {
        Run(ContinueLoadFromLoaderWithOptionalAd());
    }

    public static void Restart()
    {
        Load(GetCurrentScene(), 0.1f);
    }

    public static void ShowInterstitialThenContinue(IAdsService adsService, GameScene nextScene)
    {
        if (isLoading)
            return;

        pendingAdService = adsService;
        Load(nextScene);
    }

    #endregion

    #region Core Loading Logic

    private static IEnumerator LoadSceneAsync(GameScene targetScene, float delay)
    {
        string targetName = targetScene.ToString();
        isLoading = true;
        OnSceneLoadStarted?.Invoke();

        try
        {
            while (SceneManager.GetActiveScene().name != GameScene.Loader.ToString())
                yield return null;

            if (delay > 0f)
                yield return new WaitForSecondsRealtime(delay);

            if (!Application.CanStreamedLevelBeLoaded(targetName))
                yield break;

            Scene existing = SceneManager.GetSceneByName(targetName);
            if (existing.IsValid() && existing.isLoaded)
                yield return SceneManager.UnloadSceneAsync(existing);

            loadingAsyncOperation = SceneManager.LoadSceneAsync(targetName, LoadSceneMode.Additive);
            loadingAsyncOperation.allowSceneActivation = true;

            while (!loadingAsyncOperation.isDone)
                yield return null;

            Scene loadedScene = SceneManager.GetSceneByName(targetName);
            if (loadedScene.IsValid() && loadedScene.isLoaded)
                SceneManager.SetActiveScene(loadedScene);

            yield return SceneManager.UnloadSceneAsync(GameScene.Loader.ToString());
        }
        finally
        {
            loadingAsyncOperation = null;
            isLoading = false;
        }
    }

    private static IEnumerator ContinueLoadFromLoaderWithOptionalAd()
    {
#if UNITY_EDITOR
        if (!targetScene.HasValue)
        {
            if (!Enum.TryParse(editorStartingScene, out GameScene parsed))
                parsed = GameScene.MainMenu;
            targetScene = parsed == GameScene.Loader ? GameScene.MainMenu : parsed;
        }
#else
        if (!targetScene.HasValue)
            targetScene = GameScene.MainMenu;
#endif

        if (pendingAdService != null)
        {
            while (!pendingAdService.IsInitialized)
                yield return null;

            float waitReadyTimer = 0f;
            const float waitReadyTimeout = 6f;

            while (!pendingAdService.InterstitialAdReady && waitReadyTimer < waitReadyTimeout)
            {
                waitReadyTimer += Time.unscaledDeltaTime;
                yield return null;
            }

            if (pendingAdService.InterstitialAdReady)
            {
                pendingAdService.ShowInterstitialAd();

                float waitDoneTimer = 0f;
                const float waitDoneTimeout = 10f;

                while (!pendingAdService.InterstitialAdCompleted && waitDoneTimer < waitDoneTimeout)
                {
                    waitDoneTimer += Time.unscaledDeltaTime;
                    yield return null;
                }

                if (!pendingAdService.InterstitialAdCompleted)
                {
                    pendingAdService.ForceMarkAdComplete();
                }
            }

            pendingAdService = null;
        }

        yield return LoadSceneAsync(targetScene.Value, 0f);
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

    #region Internal Coroutine Runner

    private class LoaderHost : MonoBehaviour { }

    private static void Run(IEnumerator routine)
    {
        if (host == null)
        {
            var go = new GameObject("[LoaderHost]");
            UnityEngine.Object.DontDestroyOnLoad(go);
            host = go.AddComponent<LoaderHost>();
        }
        host.StartCoroutine(routine);
    }

    #endregion
}
