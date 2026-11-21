using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class SaveService : ISaveService, IStartable, IDisposable
{
    private const string SaveFileName = "save.dat";
    private const string ConfigFileName = "save-config.json";
    private const string EditorFallbackEncryptionKey = "dev-only-pocketmatch-key-0000001";

    /// <summary>
    /// Client schema version. Raise only when <see cref="DataMigrator"/> gains real migration steps.
    /// </summary>
    public const int CurrentSaveVersion = 1;

    private readonly string saveFile;
    private readonly string encryptionKey;
    private readonly ICloudSaveProvider cloud;
    private readonly Func<bool> isOnline;

    private bool cloudInitialized;
    private bool cloudSynced;
    private bool syncing;

    public PlayerData PlayerData { get; private set; }

    public CloudSyncStatus CloudSyncStatus { get; private set; } = CloudSyncStatus.NotInitialized;

    public event Action CloudSyncStatusChanged;

    public event Action PlayerDataReplaced;

    [Inject]
    public SaveService(ICloudSaveProvider cloud)
        : this(
            cloud,
            Path.Combine(Application.persistentDataPath, SaveFileName),
            ResolveEncryptionKey(),
            () => Application.internetReachability != NetworkReachability.NotReachable)
    {
    }

    public SaveService(ICloudSaveProvider cloud, string saveFile, string encryptionKey, Func<bool> isOnline)
    {
        this.cloud = cloud;
        this.saveFile = saveFile;
        this.encryptionKey = encryptionKey;
        this.isOnline = isOnline;

        Load();
        Debug.Log("[SaveService] Local save loaded");

        if (!cloud.IsEnabled)
            CloudSyncStatus = CloudSyncStatus.Disabled;
    }

    public void Start()
    {
        LevelEvents.OnLevelCompleted += OnLevelCompleted;
    }

    public void Dispose()
    {
        LevelEvents.OnLevelCompleted -= OnLevelCompleted;
    }

    private void OnLevelCompleted(object sender, LevelCompletedEventArgs e)
    {
        if (e.IsLevelCapReached)
        {
            Debug.Log("[SaveService] Level cap reached, not incrementing level index.");
            return;
        }

        if (e.CompletedLevelIndex == PlayerData.nextLevelIndex)
        {
            PlayerData.nextLevelIndex++;
            Debug.Log($"[SaveService] Progress advanced. Next unlocked level: {PlayerData.nextLevelIndex + 1}");
        }
        else
        {
            Debug.Log($"[SaveService] Replay win on level {e.CompletedLevelIndex + 1}. Progress unchanged.");
        }

        Save();
    }

    public async Task InitializeCloudAsync()
    {
        if (!cloud.IsEnabled)
            return;

        try
        {
            await cloud.InitializeAsync();
            cloudInitialized = true;
            Debug.Log("[SaveService] Cloud initialized successfully");

            if (isOnline())
            {
                await TryLoadFromCloud();
            }
            else
            {
                SetCloudSyncStatus(CloudSyncStatus.Offline);
                Debug.Log("[SaveService] Offline - skipping cloud load");
            }
        }
        catch (Exception e)
        {
            cloudInitialized = false;
            SetCloudSyncStatus(CloudSyncStatus.InitFailed);
            Debug.LogWarning("[SaveService] Cloud initialization failed: " + e);
        }
    }

    public void Load()
    {
        PlayerData = LoadFile(saveFile) ?? new PlayerData();
        EnsureSaveVersion();
    }

    public void Save() => _ = SaveAsync();

    public void ResetToDefaults() => _ = ResetToDefaultsAsync();

    /// <summary>Writes the local save, then pushes it to the cloud when the sync rules allow.</summary>
    public Task SaveAsync()
    {
        PlayerData.meta.lastSaveTime = DateTime.UtcNow.ToString("o");

        SaveLocalOnly();
        return PushToCloudAsync(overwrite: false);
    }

    /// <summary>Replaces the save with a fresh one locally and in the cloud, skipping the conflict rule.</summary>
    public Task ResetToDefaultsAsync()
    {
        PlayerData = new PlayerData();
        PlayerData.meta.saveVersion = CurrentSaveVersion;
        SaveLocalOnly();

        return PushToCloudAsync(overwrite: true);
    }

    private void SaveLocalOnly()
    {
        WriteFile(saveFile, PlayerData);
    }

    /// <summary>
    /// Uploads once the first download has been compared with local. Until then a save triggers that comparison
    /// instead, so a fresh local save cannot overwrite a better cloud copy. <paramref name="overwrite"/> skips it.
    /// </summary>
    private async Task PushToCloudAsync(bool overwrite)
    {
        if (!cloudInitialized)
            return;

        if (!isOnline())
        {
            SetCloudSyncStatus(CloudSyncStatus.Offline);
            return;
        }

        if (overwrite)
            cloudSynced = true;

        if (!cloudSynced)
        {
            await TryLoadFromCloud();
            return;
        }

        try
        {
            await cloud.UploadAsync(PlayerData);
            SetCloudSyncStatus(CloudSyncStatus.Ready);
            Debug.Log("[SaveService] Cloud save uploaded");
        }
        catch (Exception e)
        {
            SetCloudSyncStatus(CloudSyncStatus.UploadFailed);
            Debug.LogWarning("[SaveService] Cloud upload failed - local save still OK: " + e);
        }
    }

    private async Task TryLoadFromCloud()
    {
        if (!cloudInitialized || syncing)
            return;

        syncing = true;
        try
        {
            var cloudData = await cloud.DownloadAsync();

            if (SaveConflictResolver.CloudWins(PlayerData, cloudData))
            {
                ApplyCloudOverLocal(cloudData);
                Debug.Log("[SaveService] Cloud save is ahead - applied over local");
            }
            else
            {
                await cloud.UploadAsync(PlayerData);
                SetCloudSyncStatus(CloudSyncStatus.Ready);
                Debug.Log("[SaveService] Local save is ahead or equal - uploaded to cloud");
            }

            cloudSynced = true;
        }
        catch (Exception e)
        {
            SetCloudSyncStatus(CloudSyncStatus.InitFailed);
            Debug.LogWarning("[SaveService] Cloud sync failed - keeping local save: " + e);
        }
        finally
        {
            syncing = false;
        }
    }

    private void ApplyCloudOverLocal(PlayerData cloudData)
    {
        PlayerData = cloudData;
        EnsureSaveVersion();
        SaveLocalOnly();
        SetCloudSyncStatus(CloudSyncStatus.AppliedFromCloud);
        PlayerDataReplaced?.Invoke();
    }

    private void EnsureSaveVersion()
    {
        if (PlayerData?.meta == null)
            return;

        int onDisk = PlayerData.meta.saveVersion;
        if (onDisk < CurrentSaveVersion)
        {
            PlayerData = DataMigrator.Migrate(PlayerData, onDisk, CurrentSaveVersion);
            SaveLocalOnly();
        }
        else if (onDisk > CurrentSaveVersion)
        {
            Debug.LogWarning(
                $"[SaveService] Save schema v{onDisk} is newer than client v{CurrentSaveVersion}. Loading as-is.");
        }
    }

    private void SetCloudSyncStatus(CloudSyncStatus status)
    {
        if (CloudSyncStatus == status)
            return;

        CloudSyncStatus = status;
        CloudSyncStatusChanged?.Invoke();
    }

    private static string ResolveEncryptionKey()
    {
        if (LocalJsonConfig.TryLoad(ConfigFileName, out SaveConfig config) &&
            config != null &&
            config.IsValid)
        {
            return config.encryptionKey.Trim();
        }

        Debug.LogWarning(
            "[SaveService] save-config.json missing or invalid. Using a development fallback encryption key.");
        return EditorFallbackEncryptionKey;
    }

    private PlayerData LoadFile(string path)
    {
        var data = TryReadFile(path);
        if (data != null)
            return data;

        var backup = TryReadFile(AtomicFile.BackupPath(path));
        if (backup != null)
            Debug.LogWarning($"[SaveService] {Path.GetFileName(path)} unreadable or missing - restored from backup");

        return backup;
    }

    private PlayerData TryReadFile(string path)
    {
        try
        {
            if (!File.Exists(path))
                return null;

            string encrypted = File.ReadAllText(path);
            string json = SaveCrypto.Decrypt(encrypted, encryptionKey);
            return JsonUtility.FromJson<PlayerData>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load file {path}: {e}");
            return null;
        }
    }

    private void WriteFile(string path, PlayerData data)
    {
        try
        {
            string json = JsonUtility.ToJson(data, true);
            string encrypted = SaveCrypto.Encrypt(json, encryptionKey);
            AtomicFile.Write(path, encrypted);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save file {path}: {e}");
        }
    }
}
