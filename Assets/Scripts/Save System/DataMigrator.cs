using UnityEngine;

public static class DataMigrator
{
    public static PlayerData Migrate(PlayerData oldData, int oldVersion, int newVersion)
    {
        Debug.Log($"Migrating save from v{oldVersion} to v{newVersion}");

        oldData.meta.saveVersion = newVersion;

        return oldData;
    }
}