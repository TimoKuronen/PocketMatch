using UnityEngine;

[CreateAssetMenu(fileName = "Match3Profile", menuName = "Performance/Match3")]
public class Match3PerformanceProfile : BasePerformanceProfile
{
    public override void Apply()
    {
        base.Apply();
        QualitySettings.shadows = ShadowQuality.Disable;
    }
}
