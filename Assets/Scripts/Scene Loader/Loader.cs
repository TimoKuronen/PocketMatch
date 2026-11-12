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

#if !UNITY_EDITOR
    private static async UniTask WaitWithTimeoutAsync(Func<bool> condition, float timeoutSeconds)
    {
        float elapsed = 0f;

        while (!condition() && elapsed < timeoutSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            await UniTask.Yield();
        }
    }
#endif

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
        if (AdsDebugConfig.SkipInterstitials)
            pendingAdService = null;

        if (pendingAdService != null)
        {
#if UNITY_EDITOR
            // AdsService simulates completion in the Editor, so there is nothing to wait for.
            pendingAdService.ShowInterstitialAd();
#else
            const float waitInitTimeout = 3f;
            const float waitReadyTimeout = 6f;
            const float waitDoneTimeout = 10f;

            var adService = pendingAdService;
            await WaitWithTimeoutAsync(() => adService.IsInitialized, waitInitTimeout);

            if (adService.IsInitialized)
                await WaitWithTimeoutAsync(() => adService.InterstitialAdReady, waitReadyTimeout);

            bool canShow = adService.IsInitialized && adService.InterstitialAdReady;

            // Also called when no ad can show, so AdsService records the ad_skipped reason.
            adService.ShowInterstitialAd();

            if (canShow)
            {
                await WaitWithTimeoutAsync(() => adService.InterstitialAdCompleted, waitDoneTimeout);

                if (!adService.InterstitialAdCompleted)
                    adService.ForceMarkAdComplete();
            }
#endif

            pendingAdService = null;
        }

        // Ensure the loader scene is visible for at least half a second
        await LoadSceneAsync(targetScene.Value, 0.5f);
    }

    #endregion

    #region Private Helpers

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
