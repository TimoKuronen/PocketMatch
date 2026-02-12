using UnityEngine;

public class PerformanceManager : MonoBehaviour
{
    [SerializeField] private BasePerformanceProfile lowEnd;
    [SerializeField] private BasePerformanceProfile highEnd;

    void Awake()
    {
        // If device has less than 3GB RAM, use Low
        if (SystemInfo.systemMemorySize < 3000)
        {
            lowEnd.Apply();
        }
        else
        {
            highEnd.Apply();
        }
    }
}