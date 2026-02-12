using UnityEngine;

public abstract class BasePerformanceProfile : ScriptableObject
{
    public int targetFrameRate = 60;
    public int vSyncCount = 1;

    public virtual void Apply()
    {
        Application.targetFrameRate = targetFrameRate;
        QualitySettings.vSyncCount = vSyncCount;
    }
}