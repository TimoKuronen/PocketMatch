using UnityEngine;

public interface IEffectService
{
    void PlayEffect(string effectKey, Vector3 position, Quaternion rotation = default);
    void PreloadEffects(string[] effectKeys);
    void PreloadEffectsByLabel(string label);
    void ReleaseEffect(GameObject effectInstance);
}
