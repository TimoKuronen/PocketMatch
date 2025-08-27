using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class GameSessionService : IGameSessionService
{
    private const string defaultAddress = "Assets/Addressables/Levels/MapData_";

    private string address = "MapDataAddress";
    public MapData CurrentMapData { get; private set; }
    public bool IsLevelDataLoaded { get; private set; }

    private ISaveService saveService;

    public void Initialize()
    {
        saveService = Services.Get<ISaveService>();

        SetLevelAddress();

        Addressables.LoadAssetAsync<MapData>(address).Completed += OnMapDataLoaded;
    }

    private void SetLevelAddress()
    {
        string levelIntegerString = saveService.PlayerData.nextLevelIndex.ToString();
        int length = levelIntegerString.Length;

        if (levelIntegerString == "0")
            length = 0;

        Debug.Log("Setting level address for level: " + levelIntegerString + " (length: " + length + ")");

        switch (length)
        {
            case 0:
                address = defaultAddress + "0002.asset";
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

    void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            saveService.Save();
            saveService.SaveSettings();
        }
    }

    void OnApplicationQuit()
    {
        saveService.Save();
        saveService.SaveSettings();
    }

    public void Dispose()
    {
        Addressables.LoadAssetAsync<MapData>(address).Completed -= OnMapDataLoaded;
    }
}
