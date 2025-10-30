using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GameSettingsService
{
    public void Initialize()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        QualitySettings.antiAliasing = 0;
        QualitySettings.shadows = UnityEngine.ShadowQuality.Disable;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;

        // URP-only extras
        if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
        {
            urp.supportsHDR = false;
            urp.msaaSampleCount = 2;
            urp.supportsCameraDepthTexture = false;
            urp.shadowDistance = 0f;
            urp.renderScale = 0.85f;
        }
    }

    public void Dispose() { }
}
