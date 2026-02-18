using UnityEngine;

public interface IEffectService
{
    void PlayEffect(string effectKey, Vector3 position, Quaternion rotation = default);
}
