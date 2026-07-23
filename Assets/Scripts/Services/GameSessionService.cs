using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using VContainer;

public class GameSessionService : IGameSessionService, IDisposable
{
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
        totalLevels = await LevelCatalog.GetTotalLevelsAsync();
        Debug.Log($"[GameSessionService] Total levels found: {totalLevels}");
    }

    public async UniTask LoadCurrentLevelDataAsync()
    {
        // release any previously loaded level to avoid memory buildup
        if (currentHandle.HasValue)
        {
            Addressables.Release(currentHandle.Value);
            currentHandle = null;
        }

        int levelIndex = GameSignals.ResolveLevelIndex(saveService.PlayerData.nextLevelIndex);
        GameSignals.ConsumePendingLevelIndex();

        string address = LevelCatalog.GetAddress(levelIndex);

        try
        {
            currentHandle = Addressables.LoadAssetAsync<MapData>(address);
            CurrentMapData = await currentHandle.Value.Task;

            Debug.Log($"[GameSessionService] MapData loaded: {CurrentMapData.name} (index {levelIndex})");

            int levelNumber = levelIndex + 1;
            IsLevelCapReached = levelNumber >= totalLevels;
            GameSignals.SetActiveLevelIndex(levelIndex);
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