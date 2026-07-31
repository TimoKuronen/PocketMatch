using UnityEngine;

[CreateAssetMenu(fileName = "PowerVfxSettings", menuName = "ScriptableObjects/PowerVfxSettings", order = 2)]
public class PowerVfxSettings : ScriptableObject
{
    [Tooltip("Total time for the stagger wave to cover all affected tiles.")]
    public float spreadDuration = 0.5f;

    [Tooltip("Per-tile shrink duration after each mini burst starts.")]
    public float destroyDuration = 0.4f;

    [Tooltip("Minimum scale applied to each mini explosion instance.")]
    public float scaleJitterMin = 0.85f;

    [Tooltip("Maximum scale applied to each mini explosion instance.")]
    public float scaleJitterMax = 1.0f;
}
