using System.Threading.Tasks;

public interface ISaveService
{
    PlayerData PlayerData { get; }

    void Load();
    void Save();
    void ResetToDefaults();

    Task UploadCloudAsync();
    Task DownloadCloudAsync();
    Task InitializeCloudAsync();
}