using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class LevelCatalog
{
    private const string LevelAddressPrefix = "Assets/Addressables/Levels/MapData_";

    public static string GetAddress(int zeroBasedIndex)
    {
        int levelNumber = zeroBasedIndex + 1;
        return $"{LevelAddressPrefix}{levelNumber:D4}.asset";
    }

    public static async UniTask<int> GetTotalLevelsAsync()
    {
        var handle = Addressables.LoadResourceLocationsAsync("Levels", typeof(MapData));
        await handle.Task;

        int totalLevels = 0;
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            totalLevels = handle.Result.Count;
        }
        else
        {
            Debug.LogError("[LevelCatalog] Failed to load level locations.");
        }

        Addressables.Release(handle);
        return totalLevels;
    }
}
