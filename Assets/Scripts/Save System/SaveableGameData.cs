public static class SaveableGameData
{
    public static void SaveLevelProgression()
    {
        SaveManager.UpdateSave(data =>
        {
            data.NextLevelIndex++;
        });
    }

    public static int GetNextLevel()
    {
        return SaveManager.Read(data => data.NextLevelIndex);
    }
}
