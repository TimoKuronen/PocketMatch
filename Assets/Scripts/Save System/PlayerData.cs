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
    public int saveVersion = 1;
    public string lastSaveTime;
    public string installId;
}