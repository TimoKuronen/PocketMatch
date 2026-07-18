using UnityEngine;

public interface IAudioService
{
    float SfxVolume { get; set; }
    void Play(AudioCue data, AudioSource audioSource);
    void PlayExclusive(AudioCue data, AudioSource audioSource);
}
