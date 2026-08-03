using System;

[Serializable]
public class SaveConfig
{
    public string encryptionKey;

    public bool IsValid =>
        !string.IsNullOrWhiteSpace(encryptionKey) &&
        encryptionKey.Length == 32;
}
