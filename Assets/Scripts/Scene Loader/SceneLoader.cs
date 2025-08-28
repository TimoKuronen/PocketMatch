using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : ISceneLoader
{
    public enum GameScene
    {
        MainMenu,
        Loader,
        Game
    }

    public event Action OnSceneLoadStarted;
    public event Action OnSceneLoadCompleted;

    private AsyncOperation loadingAsyncOperation;
    private GameScene targetScene;

    public void Initialize()
    {

    }

    public bool IsGameScene()
    {
        return GetCurrentScene() != GameScene.Loader && GetCurrentScene() != GameScene.MainMenu;
    }

    public bool IsMenuScene()
    {
        return GetCurrentScene() == GameScene.MainMenu;
    }

    public GameScene GetCurrentScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        foreach (GameScene scene in Enum.GetValues(typeof(GameScene)))
        {
            if (SceneConfig.GetSceneName(scene) == sceneName)
                return scene;
        }
        return GameScene.Loader;
    }

    public void LoadScene(GameScene scene, bool additive = false)
    {
        targetScene = scene;
        OnSceneLoadStarted?.Invoke();

        if (additive)
        {
            SceneManager.LoadScene(SceneConfig.GetSceneName(scene), LoadSceneMode.Additive);
            OnSceneLoadCompleted?.Invoke();
        }
        else
        {
            CoroutineMonoBehavior.Instance.StartCoroutine(LoadSceneAsync(scene));
        }
    }

    public IEnumerator LoadSceneAsync(GameScene scene)
    {
        string sceneName = SceneConfig.GetSceneName(scene);

        loadingAsyncOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

        while (!loadingAsyncOperation.isDone)
        {
            yield return null;
        }

        OnSceneLoadCompleted?.Invoke();
    }

    public void RestartScene()
    {
        LoadScene(GetCurrentScene());
    }

    public float GetLoadingProgress()
    {
        return loadingAsyncOperation?.progress ?? 1f;
    }

    public void Dispose() { }
}