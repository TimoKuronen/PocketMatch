using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;

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
        ContinueLoadFromLoaderWithOptionalAdAsync().Forget();
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

    private static async UniTask LoadSceneAsync(GameScene targetScene, float delay)
    {
        string targetName = targetScene.ToString();
        isLoading = true;
        OnSceneLoadStarted?.Invoke();

        try
        {
            while (SceneManager.GetActiveScene().name != GameScene.Loader.ToString())
                await UniTask.Yield();

            // Ensure the loader scene is visible for at least the specified delay,
            // even if the target scene loads very quickly.
            if (delay > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(delay), DelayType.Realtime);

            if (!Application.CanStreamedLevelBeLoaded(targetName))
                return;

            Scene existing = SceneManager.GetSceneByName(targetName);
            if (existing.IsValid() && existing.isLoaded)
            {
                var unloadExistingOp = SceneManager.UnloadSceneAsync(existing);
                if (unloadExistingOp != null)
                {
                    await unloadExistingOp.ToUniTask();
                }
            }

            loadingAsyncOperation = SceneManager.LoadSceneAsync(targetName, LoadSceneMode.Additive);
            loadingAsyncOperation.allowSceneActivation = true;

            if (loadingAsyncOperation != null)
            {
                await loadingAsyncOperation.ToUniTask();
            }

            Scene loadedScene = SceneManager.GetSceneByName(targetName);
            if (loadedScene.IsValid() && loadedScene.isLoaded)
                SceneManager.SetActiveScene(loadedScene);

            var unloadLoaderOp = SceneManager.UnloadSceneAsync(GameScene.Loader.ToString());
            if (unloadLoaderOp != null)
            {
                await unloadLoaderOp.ToUniTask();
            }
        }
        finally
        {
            loadingAsyncOperation = null;
            isLoading = false;
        }
    }

    private static async UniTask ContinueLoadFromLoaderWithOptionalAdAsync()
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
#if UNITY_EDITOR
            // In editor, skip showing the real interstitial (which uses a Unity placeholder
            // that may not close correctly) and immediately mark it complete so loading continues.
            pendingAdService.ForceMarkAdComplete();
#else
            while (!pendingAdService.IsInitialized)
                await UniTask.Yield();

            float waitReadyTimer = 0f;
            const float waitReadyTimeout = 6f;

            while (!pendingAdService.InterstitialAdReady && waitReadyTimer < waitReadyTimeout)
            {
                waitReadyTimer += Time.unscaledDeltaTime;
                await UniTask.Yield();
            }

            if (pendingAdService.InterstitialAdReady)
            {
                pendingAdService.ShowInterstitialAd();

                float waitDoneTimer = 0f;
                const float waitDoneTimeout = 10f;

                while (!pendingAdService.InterstitialAdCompleted && waitDoneTimer < waitDoneTimeout)
                {
                    waitDoneTimer += Time.unscaledDeltaTime;
                    await UniTask.Yield();
                }

                if (!pendingAdService.InterstitialAdCompleted)
                {
                    pendingAdService.ForceMarkAdComplete();
                }
            }
#endif

            pendingAdService = null;
        }

        // Ensure the loader scene is visible for at least half a second
        await LoadSceneAsync(targetScene.Value, 0.5f);
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
