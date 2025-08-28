using System;
using static SceneLoader;

public static class SceneConfig
{
    public static string GetSceneName(GameScene scene)
    {
        return scene switch
        {
            GameScene.Loader => "Loader",
            GameScene.MainMenu => "MainMenu",
            GameScene.Game => "GameScene",
            _ => throw new ArgumentOutOfRangeException(nameof(scene), scene, null)
        };
    }
}