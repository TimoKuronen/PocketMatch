/// <summary>
/// Cloud sync health for UI and logs. Local save stays independent of these states.
/// </summary>
public enum CloudSyncStatus
{
    Disabled,
    NotInitialized,
    Ready,
    Offline,
    InitFailed,
    UploadFailed,
    AppliedFromCloud
}
