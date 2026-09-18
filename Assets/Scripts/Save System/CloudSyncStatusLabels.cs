/// <summary>
/// Short English labels for settings footer. Sync status is diagnostic, not game copy.
/// </summary>
public static class CloudSyncStatusLabels
{
    public static string ToDisplay(CloudSyncStatus status)
    {
        return status switch
        {
            CloudSyncStatus.NotInitialized => "Cloud: waiting",
            CloudSyncStatus.Ready => "Cloud: ready",
            CloudSyncStatus.Offline => "Cloud: offline (local only)",
            CloudSyncStatus.InitFailed => "Cloud: init failed (local only)",
            CloudSyncStatus.UploadFailed => "Cloud: upload failed (local OK)",
            CloudSyncStatus.AppliedFromCloud => "Cloud: applied over local",
            _ => "Cloud: unknown"
        };
    }
}
