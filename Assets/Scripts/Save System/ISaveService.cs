using System;
using System.Threading.Tasks;

/// <summary>
/// Local encrypted persistence for <see cref="PlayerData"/> with optional sync through <see cref="ICloudSaveProvider"/>.
/// Conflict policy: no merge. When a cloud document exists on init download, it replaces local.
/// Upload failures never corrupt or roll back the local save.
/// </summary>
public interface ISaveService
{
    PlayerData PlayerData { get; }

    CloudSyncStatus CloudSyncStatus { get; }

    event Action CloudSyncStatusChanged;

    void Load();
    void Save();
    void ResetToDefaults();

    /// <summary>Initializes the cloud provider and attempts a download when the device is online.</summary>
    Task InitializeCloudAsync();
}
