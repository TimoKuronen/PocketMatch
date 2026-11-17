using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using UnityEngine;

/// <summary>
/// Stores the player save as one JSON item in Unity Cloud Save. Players sign in anonymously, so the save
/// follows the install, not a user account.
/// </summary>
public sealed class UgsCloudSaveProvider : ICloudSaveProvider
{
    private const string SaveKey = "player_data";

    private readonly IUgsInitializer ugs;

    public UgsCloudSaveProvider(IUgsInitializer ugs)
    {
        this.ugs = ugs;
    }

    public bool IsEnabled => true;

    public async Task InitializeAsync()
    {
        if (!await ugs.InitializeAsync())
            throw new InvalidOperationException("Unity Gaming Services failed to initialize.");

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public async Task UploadAsync(PlayerData data)
    {
        var item = new Dictionary<string, object> { [SaveKey] = JsonUtility.ToJson(data) };
        await CloudSaveService.Instance.Data.Player.SaveAsync(item);
    }

    public async Task<PlayerData> DownloadAsync()
    {
        var items = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { SaveKey });
        if (!items.TryGetValue(SaveKey, out var item))
            return null;

        return JsonUtility.FromJson<PlayerData>(item.Value.GetAsString());
    }
}
