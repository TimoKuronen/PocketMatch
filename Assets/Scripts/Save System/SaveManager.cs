using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public static class SaveManager
{
    private static readonly string saveFile = Path.Combine(Application.persistentDataPath, "save.dat");
    private static readonly string settingsFile = Path.Combine(Application.persistentDataPath, "settings.dat");
    private static readonly string encryptionKey = "kg8hv4j08jiikloijvbjmnhuj8945dxz"; // must be 32 bytes for AES-256
    
    public static void Save(SaveData data)
    {
        try
        {
            string json = JsonUtility.ToJson(data);
            string encrypted = Encrypt(json, encryptionKey);
            File.WriteAllText(saveFile, encrypted);
        }
        catch (Exception e)
        {
            Debug.LogError("Save failed: " + e);
        }
    }

    public static SaveData Load()
    {
        try
        {
            if (!File.Exists(saveFile))
            {
                //Debug.Log("new savefile created");
                return new SaveData();
            }

            string encrypted = File.ReadAllText(saveFile);
            string json = Decrypt(encrypted, encryptionKey);
            return JsonUtility.FromJson<SaveData>(json);
        }
        catch (Exception e)
        {
            Debug.LogError("Load failed: " + e);
            return new SaveData();
        }
    }

    public static void UpdateSave(Action<SaveData> modifyCallback)
    {
        SaveData current = Load();
        modifyCallback?.Invoke(current);
        Save(current);
    }

    public static T Read<T>(Func<SaveData, T> selector)
    {
        SaveData data = Load();
        return selector(data);
    }

    public static T Read<T>(Func<SaveData, T> selector, T defaultValue)
    {
        try
        {
            return selector(Load());
        }
        catch
        {
            return defaultValue;
        }
    }

    public static void SaveSettings(SettingsData data)
    {
        try
        {
            string json = JsonUtility.ToJson(data);
            string encrypted = Encrypt(json, encryptionKey);
            File.WriteAllText(settingsFile, encrypted);
        }
        catch (Exception e)
        {
            Debug.LogError("Settings save failed: " + e);
        }
    }

    public static SettingsData LoadSettings()
    {
        try
        {
            if (!File.Exists(settingsFile))
                return new SettingsData();

            string encrypted = File.ReadAllText(settingsFile);
            string json = Decrypt(encrypted, encryptionKey);
            return JsonUtility.FromJson<SettingsData>(json);
        }
        catch (Exception e)
        {
            Debug.LogError("Settings load failed: " + e);
            return new SettingsData();
        }
    }

    public static T ReadSettings<T>(Func<SettingsData, T> selector)
    {
        SettingsData data = LoadSettings();
        return selector(data);
    }

    public static T ReadSettings<T>(Func<SettingsData, T> selector, T defaultValue)
    {
        try
        {
            return selector(LoadSettings());
        }
        catch
        {
            return defaultValue;
        }
    }

    public static void DeleteSave()
    {
        if (File.Exists(saveFile))
        {
            Debug.Log("Deleting save file: " + saveFile);
            File.Delete(saveFile);
        }
        else
            Debug.Log("No save file to delete.");
    }

    public static void DeleteSettings()
    {
        if (File.Exists(settingsFile))
        {
            Debug.Log("Deleting settings file: " + settingsFile);
            File.Delete(settingsFile);
        }
        else
            Debug.Log("No settings file to delete.");
    }

    private static string Encrypt(string plainText, string key)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] iv = new byte[16]; // AES needs 16-byte IV
        using var aes = Aes.Create();
        aes.Key = keyBytes;
        aes.IV = iv;

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }
        return Convert.ToBase64String(ms.ToArray());
    }

    private static string Decrypt(string cipherText, string key)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] iv = new byte[16];
        byte[] buffer = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = keyBytes;
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(buffer);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        return sr.ReadToEnd();
    }
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