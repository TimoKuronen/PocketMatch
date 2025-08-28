using System;
using static SceneLoader;

public interface ISceneLoader : IService
{
    event Action OnSceneLoadStarted;
    event Action OnSceneLoadCompleted;
    void LoadScene(GameScene scene, bool additive = false);
    void RestartScene();
    GameScene GetCurrentScene();
    bool IsGameScene();
    bool IsMenuScene();
    float GetLoadingProgress();
}