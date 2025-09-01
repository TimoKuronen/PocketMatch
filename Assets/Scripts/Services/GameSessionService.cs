using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class GameSessionService : IGameSessionService
{
    private const string defaultAddress = "Assets/Addressables/Levels/MapData_";
    private ISaveService saveService;
    public MapData CurrentMapData { get; private set; }
    public bool IsLevelDataLoaded { get; private set; }
    public bool LevelCapReached { get; private set; }

    private int totalLevels;

    public async void Initialize()
    {
        saveService = Services.Get<ISaveService>();
        await LoadTotalLevelsAsync();
        await LoadCurrentLevelDataAsync();
        Debug.Log("GameSessionService initialized: " + saveService);
    }

    public async UniTask LoadTotalLevelsAsync()
    {
        try
        {
            // Load all resource locations that match the pattern
            var handle = Addressables.LoadResourceLocationsAsync("Levels", typeof(MapData));

            // Wait for the operation to complete
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                // Count the number of levels
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
            IsLevelDataLoaded = true;
            Debug.Log("MapData loaded: " + CurrentMapData.name);

            LevelCapReached = levelIndex >= totalLevels;
            Debug.Log($"Level cap reached: {LevelCapReached}");
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to load MapData: " + e);
            IsLevelDataLoaded = false;
        }
    }
    public void Dispose() { }
}