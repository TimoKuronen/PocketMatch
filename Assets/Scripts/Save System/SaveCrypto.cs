using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// AES text encryption for the local save file. The IV is fixed, so this obscures the file rather than
/// protecting it; the key must be 16, 24, or 32 bytes of UTF-8.
/// </summary>
public static class SaveCrypto
{
    public static string Encrypt(string plainText, string key)
    {
        using var aes = CreateAes(key);
        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    public static string Decrypt(string cipherText, string key)
    {
        byte[] buffer = Convert.FromBase64String(cipherText);

        using var aes = CreateAes(key);
        using var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(buffer);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);

        return sr.ReadToEnd();
    }

    private static Aes CreateAes(string key)
    {
        var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key);
        aes.IV = new byte[16];
        return aes;
    }
}
