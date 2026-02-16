using UnityEngine;

public abstract class BasePerformanceProfile : ScriptableObject
{
    [Header("Frame / Sync")]
    public int targetFrameRate = 60;

    [Header("Rendering")]
    [Tooltip("0.5–1 for low-end, 1+ for high-end.")]
    [Range(0.25f, 2f)]
    public float lodBias = 1f;

    public virtual void Apply()
    {
        Debug.Log($"Applying {name} performance profile: Target FPS = {targetFrameRate}, LOD bias = {lodBias}");
        Application.targetFrameRate = targetFrameRate;
        QualitySettings.lodBias = lodBias;
    }
}