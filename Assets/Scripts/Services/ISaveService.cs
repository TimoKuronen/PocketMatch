public interface ISaveService : IService
{
    PlayerData PlayerData { get; }
    SettingsData Settings { get; }

    void Load();
    void Save();
    void SaveSettings();
    void ResetToDefaults();
}

