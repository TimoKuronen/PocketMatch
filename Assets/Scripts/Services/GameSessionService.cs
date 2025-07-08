using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class GameSessionService : IGameSessionService
{
    private const string defaultAddress = "Assets/Addressables/Levels/MapData_";

    private string address = "MapDataAddress";
    public MapData CurrentMapData { get; private set; }
    public bool IsLevelDataLoaded { get; private set; }

    public void Initialize()
    {
        SetLevelAddress();

        Addressables.LoadAssetAsync<MapData>(address).Completed += OnMapDataLoaded;
    }

    private void SetLevelAddress()
    {
        string levelIntegerString = SaveManager.Read(data => data.NextLevelIndex.ToString(), "0");
        int length = levelIntegerString.Length;

        if (levelIntegerString == "0")
            length = 0;

        Debug.Log("Setting level address for level: " + levelIntegerString + " (length: " + length + ")");

        switch (length)
        {
            case 0:
                address = defaultAddress + "0001.asset";
                break;
            case 1:
                address = defaultAddress + "000" + levelIntegerString + ".asset";
                break;
            case 2:
                address = defaultAddress + "00" + levelIntegerString + ".asset";
                break;
            case 3:
                address = defaultAddress + "0" + levelIntegerString + ".asset";
                break;
        }
    }

    private void OnMapDataLoaded(AsyncOperationHandle<MapData> handle)
    {
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            CurrentMapData = handle.Result;
            Debug.Log("MapData loaded: " + CurrentMapData.name);
            IsLevelDataLoaded = true;
        }
        else
        {
            Debug.LogError("Failed to load MapData from address.");
        }
    }

    public void Dispose()
    {
        Addressables.LoadAssetAsync<MapData>(address).Completed -= OnMapDataLoaded;
    }
}
