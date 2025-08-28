using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using UnityEngine;

public class SaveManager : ISaveService
{
    private readonly string saveFile = Path.Combine(Application.persistentDataPath, "save.dat");
    private readonly string settingsFile = Path.Combine(Application.persistentDataPath, "settings.dat");
    private readonly string encryptionKey = "kg8hv4j08jiikloijvbjmnhuj8945dxz";

    public PlayerData PlayerData { get; private set; }
    public SettingsData Settings { get; private set; }

    public void Initialize()
    {
        Load();
    }

    public void Load()
    {
        PlayerData = LoadFile<PlayerData>(saveFile) ?? new PlayerData();
        Settings = LoadFile<SettingsData>(settingsFile) ?? new SettingsData();

        if (PlayerData.meta.saveVersion < CurrentVersion)
        {
            PlayerData = DataMigrator.Migrate(PlayerData, PlayerData.meta.saveVersion, CurrentVersion);
        }

        Debug.Log($"SaveManager initialized. Save file: {PlayerData}");
    }

    public void Save()
    {
        PlayerData.meta.lastSaveTime = DateTime.UtcNow.ToString("o");
        WriteFile(saveFile, PlayerData);
    }

    public void SaveSettings()
    {
        WriteFile(settingsFile, Settings);
    }

    public void ResetToDefaults()
    {
        PlayerData = new PlayerData();
        Settings = new SettingsData();
        Save();
        SaveSettings();
    }

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

    public void Dispose()
    {
        Debug.Log("Disposing SaveManager and saving data.");
    }

    private const int CurrentVersion = 1;
}

[Serializable]
public class SaveData
{
    public int NextLevelIndex = 0;
}

[Serializable]
public class SettingsData
{
    public float masterVolume = 1f;
    public string language = "en";
}