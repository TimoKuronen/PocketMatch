public interface ISaveService
{
    PlayerData PlayerData { get; }
    SettingsData Settings { get; }

    void Load();
    void Save();
    void SaveSettings();
    void ResetToDefaults();
}

