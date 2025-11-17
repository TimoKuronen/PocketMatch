using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VContainer;

public class SaveService : ISaveService, IDisposable
{
    private readonly string saveFile = Path.Combine(Application.persistentDataPath, "save.dat");
    private readonly string settingsFile = Path.Combine(Application.persistentDataPath, "settings.dat");
    private readonly string encryptionKey = "kg8hv4j08jiikloijvbjmnhuj8945dxz";

    public PlayerData PlayerData { get; private set; }

    private CloudSaveService cloud;
    private bool cloudInitialized = false;

    // ------------------------------
    // CONSTRUCTOR (VContainer Inject)
    // ------------------------------
    [Inject]
    public void Construct()
    {
        cloud = new CloudSaveService();

        Load(); // Load local immediately (never wait for cloud)
        Debug.Log("[SaveService] Local save loaded");
    }

    // ---------------------------------------
    // Called externally by CloudSaveBootstrap
    // ---------------------------------------
    public async Task InitializeCloudAsync()
    {
        try
        {
            await cloud.InitializeAsync();
            cloudInitialized = true;
            Debug.Log("[SaveService] Cloud initialized successfully");

            // Attempt cloud load automatically
            if (Application.internetReachability != NetworkReachability.NotReachable)
            {
                await TryLoadFromCloud();
            }
            else
            {
                Debug.Log("[SaveService] Offline - skipping cloud load");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[SaveService] Cloud initialization failed: " + e);
        }
    }

    // ------------------
    // CLOUD LOAD LOGIC
    // ------------------
    private async Task TryLoadFromCloud()
    {
        if (!cloudInitialized) return;

        try
        {
            var cloudData = await cloud.DownloadAsync();

            if (cloudData != null)
            {
                PlayerData = cloudData;
                SaveLocalOnly();    // sync cloud -> local
                Debug.Log("[SaveService] Cloud save applied over local");
            }
            else
            {
                Debug.Log("[SaveService] No cloud save found - using local save");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[SaveService] Cloud load failed: " + e);
        }
    }

    // -----------------
    // CLOUD PUBLIC API
    // -----------------
    public async Task UploadCloudAsync()
    {
        if (!cloudInitialized) return;

        await cloud.UploadAsync(PlayerData);
    }

    public async Task DownloadCloudAsync()
    {
        if (!cloudInitialized) return;

        var cloudData = await cloud.DownloadAsync();
        if (cloudData != null)
        {
            PlayerData = cloudData;
            SaveLocalOnly();
        }
    }

    // -----------------
    // Local Load/Save
    // -----------------
    public void Load()
    {
        PlayerData = LoadFile<PlayerData>(saveFile) ?? new PlayerData();
    }

    // Called by gameplay (e.g. completing level)
    public async void Save()
    {
        PlayerData.meta.lastSaveTime = DateTime.UtcNow.ToString("o");

        SaveLocalOnly();

        if (cloudInitialized &&
            Application.internetReachability != NetworkReachability.NotReachable)
        {
            try
            {
                await cloud.UploadAsync(PlayerData);
                Debug.Log("[SaveService] Cloud save uploaded");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SaveService] Cloud upload failed - local save still OK: " + e);
            }
        }
    }

    private void SaveLocalOnly()
    {
        WriteFile(saveFile, PlayerData);
    }

    public async void ResetToDefaults()
    {
        PlayerData = new PlayerData();
        SaveLocalOnly();

        if (cloudInitialized &&
            Application.internetReachability != NetworkReachability.NotReachable)
        {
            await cloud.UploadAsync(PlayerData); // overwrite cloud
            Debug.Log("[SaveService] Cloud save reset to defaults.");
        }
    }

    // ------------------
    // LOCAL FILE HELPERS
    // ------------------
    private T LoadFile<T>(string path) where T : class
    {
        try
        {
            if (!File.Exists(path)) return null;

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

    public void Dispose() { }

    private const int CurrentVersion = 1;
}