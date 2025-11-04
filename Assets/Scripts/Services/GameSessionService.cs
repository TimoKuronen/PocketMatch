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
    private AsyncOperationHandle<MapData>? currentHandle;
    private int totalLevels;

    public MapData CurrentMapData { get; private set; }
    public bool IsLevelCapReached { get; private set; }

    [Inject]
    public void Construct(ISaveService saveService)
    {
        this.saveService = saveService;
        InitializeAsync().Forget();
    }

    private async UniTaskVoid InitializeAsync()
    {
        try
        {
            await LoadTotalLevelsAsync();
            await LoadCurrentLevelDataAsync();
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameSessionService] Initialization failed: {e}");
        }
    }

    public async UniTask LoadTotalLevelsAsync()
    {
        var handle = Addressables.LoadResourceLocationsAsync("Levels", typeof(MapData));
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            totalLevels = handle.Result.Count;
            Debug.Log($"[GameSessionService] Total levels found: {totalLevels}");
        }
        else
        {
            Debug.LogError("[GameSessionService] Failed to load level locations.");
        }

        Addressables.Release(handle);
    }

    public async UniTask LoadCurrentLevelDataAsync()
    {
        // release any previously loaded level to avoid memory buildup
        if (currentHandle.HasValue)
        {
            Addressables.Release(currentHandle.Value);
            currentHandle = null;
        }

        int levelIndex = saveService.PlayerData.nextLevelIndex + 1;
        string levelStr = levelIndex.ToString().PadLeft(4, '0');
        string address = $"{defaultAddress}{levelStr}.asset";

        try
        {
            currentHandle = Addressables.LoadAssetAsync<MapData>(address);
            CurrentMapData = await currentHandle.Value.Task;

            Debug.Log($"[GameSessionService] MapData loaded: {CurrentMapData.name}");

            IsLevelCapReached = levelIndex >= totalLevels;
            Debug.Log($"[GameSessionService] Level cap reached: {IsLevelCapReached}");

            GameSignals.MarkSessionLoaded();
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameSessionService] Failed to load MapData ({address}): {e}");
        }
    }

    public void Dispose()
    {
        if (currentHandle.HasValue)
        {
            Addressables.Release(currentHandle.Value);
            currentHandle = null;
        }
    }
}