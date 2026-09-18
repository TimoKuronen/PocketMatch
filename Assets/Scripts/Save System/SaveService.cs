using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class SaveService : ISaveService, IStartable, IDisposable
{
    private const string ConfigFileName = "save-config.json";
    private const string EditorFallbackEncryptionKey = "dev-only-pocketmatch-key-0000001";

    /// <summary>
    /// Client schema version. Raise only when <see cref="DataMigrator"/> gains real migration steps.
    /// </summary>
    public const int CurrentSaveVersion = 1;

    #region Fields

    private readonly string saveFile = Path.Combine(Application.persistentDataPath, "save.dat");
    private readonly string settingsFile = Path.Combine(Application.persistentDataPath, "settings.dat");
    private string encryptionKey;

    private CloudSaveService cloud;
    private bool cloudInitialized;
    private IObjectResolver objectResolver;

    public PlayerData PlayerData { get; private set; }

    public CloudSyncStatus CloudSyncStatus { get; private set; } = CloudSyncStatus.NotInitialized;

    public event Action CloudSyncStatusChanged;

    #endregion

    #region Lifecycle

    [Inject]
    public void Construct(IObjectResolver objectResolver)
    {
        this.objectResolver = objectResolver;
        encryptionKey = ResolveEncryptionKey();

        cloud = new CloudSaveService();

        Load();
        Debug.Log("[SaveService] Local save loaded");
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

    #endregion

    #region Cloud API

    public async Task InitializeCloudAsync()
    {
        try
        {
            await cloud.InitializeAsync();
            cloudInitialized = true;
            Debug.Log("[SaveService] Cloud initialized successfully");

            if (Application.internetReachability != NetworkReachability.NotReachable)
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

    public async Task UploadCloudAsync()
    {
        if (!cloudInitialized)
            return;

        try
        {
            await cloud.UploadAsync(PlayerData);
            SetCloudSyncStatus(CloudSyncStatus.Ready);
        }
        catch (Exception e)
        {
            SetCloudSyncStatus(CloudSyncStatus.UploadFailed);
            Debug.LogWarning("[SaveService] Cloud upload failed - local save still OK: " + e);
        }
    }

    public async Task DownloadCloudAsync()
    {
        if (!cloudInitialized)
            return;

        try
        {
            var cloudData = await cloud.DownloadAsync();
            if (cloudData != null)
            {
                ApplyCloudOverLocal(cloudData);
            }
        }
        catch (Exception e)
        {
            SetCloudSyncStatus(CloudSyncStatus.InitFailed);
            Debug.LogWarning("[SaveService] Cloud download failed - keeping local save: " + e);
        }
    }

    #endregion

    #region Local Persistence

    public void Load()
    {
        PlayerData = LoadFile<PlayerData>(saveFile) ?? new PlayerData();
        EnsureSaveVersion();
    }

    public async void Save()
    {
        PlayerData.meta.lastSaveTime = DateTime.UtcNow.ToString("o");

        SaveLocalOnly();

        if (!cloudInitialized ||
            Application.internetReachability == NetworkReachability.NotReachable)
        {
            if (cloudInitialized)
                SetCloudSyncStatus(CloudSyncStatus.Offline);
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

    public async void ResetToDefaults()
    {
        PlayerData = new PlayerData();
        PlayerData.meta.saveVersion = CurrentSaveVersion;
        SaveLocalOnly();

        if (!cloudInitialized ||
            Application.internetReachability == NetworkReachability.NotReachable)
        {
            if (cloudInitialized)
                SetCloudSyncStatus(CloudSyncStatus.Offline);
            return;
        }

        try
        {
            await cloud.UploadAsync(PlayerData);
            SetCloudSyncStatus(CloudSyncStatus.Ready);
            Debug.Log("[SaveService] Cloud save reset to defaults.");
        }
        catch (Exception e)
        {
            SetCloudSyncStatus(CloudSyncStatus.UploadFailed);
            Debug.LogWarning("[SaveService] Cloud reset upload failed - local defaults still OK: " + e);
        }
    }

    private void SaveLocalOnly()
    {
        WriteFile(saveFile, PlayerData);
    }

    #endregion

    #region Cloud Sync

    private async Task TryLoadFromCloud()
    {
        if (!cloudInitialized)
            return;

        try
        {
            var cloudData = await cloud.DownloadAsync();

            if (cloudData != null)
            {
                ApplyCloudOverLocal(cloudData);
                Debug.Log("[SaveService] Cloud save applied over local (no merge)");
            }
            else
            {
                SetCloudSyncStatus(CloudSyncStatus.Ready);
                Debug.Log("[SaveService] No cloud save found - using local save");
            }
        }
        catch (Exception e)
        {
            SetCloudSyncStatus(CloudSyncStatus.InitFailed);
            Debug.LogWarning("[SaveService] Cloud load failed - keeping local save: " + e);
        }
    }

    /// <summary>
    /// Conflict rule: cloud document replaces local memory and disk. No field merge.
    /// </summary>
    private void ApplyCloudOverLocal(PlayerData cloudData)
    {
        PlayerData = cloudData;
        EnsureSaveVersion();
        SaveLocalOnly();
        SetCloudSyncStatus(CloudSyncStatus.AppliedFromCloud);
    }

    #endregion

    #region Private Helpers

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

    private T LoadFile<T>(string path) where T : class
    {
        try
        {
            if (!File.Exists(path))
                return null;

            string encrypted = File.ReadAllText(path);
            string json = Decrypt(encrypted, encryptionKey);
            return JsonUtility.FromJson<T>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load file {path}: {e}");
            return null;
        }
    }

    private void WriteFile<T>(string path, T data)
    {
        try
        {
            string json = JsonUtility.ToJson(data, true);
            string encrypted = Encrypt(json, encryptionKey);
            File.WriteAllText(path, encrypted);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save file {path}: {e}");
        }
    }

    private string Encrypt(string plainText, string key)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] iv = new byte[16];

        using var aes = Aes.Create();
        aes.Key = keyBytes;
        aes.IV = iv;

        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    private string Decrypt(string cipherText, string key)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] iv = new byte[16];
        byte[] buffer = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = keyBytes;
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(buffer);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);

        return sr.ReadToEnd();
    }

    #endregion
}
