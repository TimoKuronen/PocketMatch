using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class GameSessionService : IGameSessionService
{
    private const string defaultAddress = "Assets/Addressables/Levels/MapData_";
    private ISaveService saveService;

    public MapData CurrentMapData { get; private set; }
    public bool IsLevelDataLoaded { get; private set; }

    public async void Initialize()
    {
        saveService = Services.Get<ISaveService>();
        await LoadCurrentLevelDataAsync();
        Debug.Log("GameSessionService initialized: " + saveService);
    }

    public async UniTask LoadCurrentLevelDataAsync()
    {
        //if (!Loader.IsGameScene())
        //{
        //    Debug.Log("Not a game scene, skipping MapData load.");
        //    return;
        //}

        int levelIndex = saveService.PlayerData.nextLevelIndex + 1;
        string levelStr = levelIndex.ToString().PadLeft(4, '0');
        string address = defaultAddress + levelStr + ".asset";

        try
        {
            var handle = Addressables.LoadAssetAsync<MapData>(address);
            CurrentMapData = await handle.Task;
            IsLevelDataLoaded = true;
            Debug.Log("MapData loaded: " + CurrentMapData.name);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to load MapData: " + e);
            IsLevelDataLoaded = false;
        }
    }
    public void Dispose() 
    {
        Loader.Reset();
    }

}