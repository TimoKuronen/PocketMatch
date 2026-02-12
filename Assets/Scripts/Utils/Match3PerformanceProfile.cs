using UnityEngine;

[CreateAssetMenu(fileName = "Match3Profile", menuName = "Performance/Match3")]
public class Match3PerformanceProfile : BasePerformanceProfile
{
    [Header("Match-3 Specifics")]
    public float idleTimeout = 5.0f;
    public float physicsStep = 0.05f;

    public override void Apply()
    {
        base.Apply();
        Time.fixedDeltaTime = physicsStep;
        QualitySettings.shadows = ShadowQuality.Disable;
    }
}
