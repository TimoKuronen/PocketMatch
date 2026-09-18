/// <summary>
/// Maps cloud sync status to UI string-table keys for settings footers.
/// </summary>
public static class CloudSyncStatusLabels
{
    public static string ToKey(CloudSyncStatus status)
    {
        return status switch
        {
            CloudSyncStatus.NotInitialized => LocalizationKeys.CommonCloudWaiting,
            CloudSyncStatus.Ready => LocalizationKeys.CommonCloudReady,
            CloudSyncStatus.Offline => LocalizationKeys.CommonCloudOffline,
            CloudSyncStatus.InitFailed => LocalizationKeys.CommonCloudInitFailed,
            CloudSyncStatus.UploadFailed => LocalizationKeys.CommonCloudUploadFailed,
            CloudSyncStatus.AppliedFromCloud => LocalizationKeys.CommonCloudApplied,
            _ => LocalizationKeys.CommonCloudUnknown
        };
    }
}
