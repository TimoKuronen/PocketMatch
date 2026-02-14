using UnityEngine;

public class PerformanceManager : MonoBehaviour
{
    [SerializeField] private BasePerformanceProfile lowEnd;
    [SerializeField] private BasePerformanceProfile highEnd;

    void Awake()
    {
        BasePerformanceProfile profile = null;
        // If device has less than 3GB RAM, use Low
        if (SystemInfo.systemMemorySize < 3000)
            profile = lowEnd;
        else
            profile = highEnd;

        if (profile != null)
            profile.Apply();
        else
            Debug.LogWarning("[PerformanceManager] No performance profile assigned for this tier (lowEnd or highEnd missing). Using default quality.");
    }
}