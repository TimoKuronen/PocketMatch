using System.Threading.Tasks;

/// <summary>
/// Local encrypted persistence for <see cref="PlayerData"/> with optional cloud sync after initialization.
/// </summary>
public interface ISaveService
{
    PlayerData PlayerData { get; }

    void Load();
    void Save();
    void ResetToDefaults();

    /// <summary>Initializes cloud backend and attempts a download when the device is online.</summary>
    Task InitializeCloudAsync();

    /// <summary>Pushes the current player data to cloud storage; no-op if cloud is not initialized.</summary>
    Task UploadCloudAsync();

    /// <summary>Replaces in-memory player data with the cloud copy when one exists, then persists locally.</summary>
    Task DownloadCloudAsync();
}
