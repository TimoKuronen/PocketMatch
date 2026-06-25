using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PerformanceManager : MonoBehaviour
{
    [SerializeField] private BasePerformanceProfile lowEnd;
    [SerializeField] private BasePerformanceProfile highEnd;

    private void Awake()
    {
        ApplySharedOptimizations();

        BasePerformanceProfile profile = SystemInfo.systemMemorySize < 3000 ? lowEnd : highEnd;
        if (profile != null)
            profile.Apply();
        else
            Debug.LogWarning("[PerformanceManager] No performance profile assigned for this tier (lowEnd or highEnd missing). Using default quality.");
    }

    private static void ApplySharedOptimizations()
    {
        QualitySettings.antiAliasing = 0;
        QualitySettings.shadows = UnityEngine.ShadowQuality.Disable;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
        QualitySettings.pixelLightCount = 0;
        QualitySettings.realtimeReflectionProbes = false;
        QualitySettings.maxQueuedFrames = 2;

        if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
        {
            urp.supportsHDR = false;
            urp.msaaSampleCount = 2;
            urp.supportsCameraDepthTexture = false;
            urp.shadowDistance = 0f;
            urp.renderScale = 0.85f;
        }
    }
}