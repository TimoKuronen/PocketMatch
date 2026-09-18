using UnityEngine;

/// <summary>
/// Advances <see cref="MetaData.saveVersion"/> when the on-disk schema lags behind the client.
/// Current policy: bump the version field only; field defaults cover additive PlayerData changes.
/// Destructive renames or removals need explicit steps here before CurrentVersion is raised.
/// </summary>
public static class DataMigrator
{
    public static PlayerData Migrate(PlayerData oldData, int oldVersion, int newVersion)
    {
        Debug.Log($"[DataMigrator] Migrating save from v{oldVersion} to v{newVersion}");

        oldData.meta.saveVersion = newVersion;

        return oldData;
    }
}
