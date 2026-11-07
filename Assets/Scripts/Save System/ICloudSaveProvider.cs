using System.Threading.Tasks;

/// <summary>
/// Stores <see cref="PlayerData"/> in a cloud backend. <see cref="SaveService"/> owns sync status and the conflict rule.
/// </summary>
public interface ICloudSaveProvider
{
    /// <summary>False when no backend is configured; the game then runs on the local save only.</summary>
    bool IsEnabled { get; }

    Task InitializeAsync();

    Task UploadAsync(PlayerData data);

    /// <summary>Returns the stored data, or null when the player has no cloud save.</summary>
    Task<PlayerData> DownloadAsync();
}
