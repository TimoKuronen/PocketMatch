using System.Threading.Tasks;

/// <summary>
/// Used when no cloud backend is configured, so saves stay on the device.
/// </summary>
public sealed class NullCloudSaveProvider : ICloudSaveProvider
{
    public bool IsEnabled => false;

    public Task InitializeAsync() => Task.CompletedTask;

    public Task UploadAsync(PlayerData data) => Task.CompletedTask;

    public Task<PlayerData> DownloadAsync() => Task.FromResult<PlayerData>(null);
}
