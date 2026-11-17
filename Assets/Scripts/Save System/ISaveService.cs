using System;
using System.Threading.Tasks;

/// <summary>
/// Local encrypted persistence for <see cref="PlayerData"/> with optional sync through <see cref="ICloudSaveProvider"/>.
/// Conflict policy: no merge. The copy with more levels unlocked wins; equal progress goes to the newer save
/// (<see cref="SaveConflictResolver"/>).
/// Upload failures never corrupt or roll back the local save.
/// </summary>
public interface ISaveService
{
    PlayerData PlayerData { get; }

    CloudSyncStatus CloudSyncStatus { get; }

    event Action CloudSyncStatusChanged;

    /// <summary>Raised when a cloud copy replaces the in-memory save, so views showing coins or progress can refresh.</summary>
    event Action PlayerDataReplaced;

    void Load();
    void Save();
    void ResetToDefaults();

    /// <summary>Initializes the cloud provider and attempts a download when the device is online.</summary>
    Task InitializeCloudAsync();
}
