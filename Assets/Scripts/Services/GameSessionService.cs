using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using VContainer;

public class GameSessionService : IGameSessionService, IDisposable
{
    private const string defaultAddress = "Assets/Addressables/Levels/MapData_";
    private ISaveService saveService;
    public MapData CurrentMapData { get; private set; }
    public bool IsLevelCapReached { get; private set; }

    private int totalLevels;

    [Inject]
    public async void Construct(ISaveService saveService)
    {
        Debug.Log("GameSessionService Construct called with service " + saveService);
        this.saveService = saveService;

        await LoadTotalLevelsAsync();
        await LoadCurrentLevelDataAsync();
    }

    public async UniTask LoadTotalLevelsAsync()
    {
        try
        {
            var handle = Addressables.LoadResourceLocationsAsync("Levels", typeof(MapData));

            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                totalLevels = handle.Result.Count;
                Debug.Log($"Total levels found: {totalLevels}");
            }
            else
            {
                Debug.LogError("Failed to load level locations.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to load level locations: " + e);
        }
    }

    public async UniTask LoadCurrentLevelDataAsync()
    {
        int levelIndex = saveService.PlayerData.nextLevelIndex + 1;
        string levelStr = levelIndex.ToString().PadLeft(4, '0');
        string address = defaultAddress + levelStr + ".asset";

        try
        {
            var handle = Addressables.LoadAssetAsync<MapData>(address);
            CurrentMapData = await handle.Task;
            Debug.Log("MapData loaded: " + CurrentMapData.name);

            IsLevelCapReached = levelIndex >= totalLevels;
            Debug.Log($"Level cap reached: {IsLevelCapReached}");

            GameSignals.MarkSessionLoaded();
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to load MapData: " + e);
        }
    }
    public void Dispose() 
    {
        Debug.Log("[GameSessionService] Disposed: " + GetHashCode());
    }
}