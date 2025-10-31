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

#if UNITY_EDITOR
    private static string editorStartingScene => EditorBootstrapper.CurrentSceneName;
#endif

    #region Public

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
        Run(ContinueLoadFromLoader());
    }

    private static IEnumerator ContinueLoadFromLoader()
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
        yield return LoadSceneAsync(targetScene.Value, 0f);
    }

    public static void Restart()
    {
        Load(GetCurrentScene(), 0.1f);
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
            adsService.ForceMarkAdComplete();

        Load(sceneToLoad);
    }

    #endregion

    #region Core

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

    #region Internal Runner

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
