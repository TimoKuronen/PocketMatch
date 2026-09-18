[System.Serializable]
public class PlayerData
{
    public MetaData meta = new MetaData();
    public int nextLevelIndex = 0;
    public int coins = 0;
}

[System.Serializable]
public class MetaData
{
    /// <summary>Schema version for <see cref="DataMigrator"/>. Not the player-facing app version.</summary>
    public int saveVersion = 1;

    /// <summary>UTC ISO timestamp of the last successful local write. Not used for cloud conflict resolution.</summary>
    public string lastSaveTime;

    public string installId;
}
