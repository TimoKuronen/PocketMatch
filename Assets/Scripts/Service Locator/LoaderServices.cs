using UnityEngine;
using static SceneLoader;

public class LoaderServices : Services
{
    protected override void Initialize()
    {
        var sceneLoader = new SceneLoader();
        AddService<ISceneLoader>(sceneLoader, isGlobal: true);

        var saveManager = new SaveManager();
        AddService<ISaveService>(saveManager, isGlobal: true);

        foreach (var service in globalServices.Values)
        {
            Debug.Log("Initializing global service: " + service.GetType().Name);
            service.Initialize();
        }

        foreach (var service in serviceMap.Values)
        {
            Debug.Log("Initializing service: " + service.GetType().Name);
            service.Initialize();
        }

        sceneLoader.LoadScene(GameScene.MainMenu, additive: true);
    }
}