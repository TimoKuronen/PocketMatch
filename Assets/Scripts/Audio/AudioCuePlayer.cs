using UnityEngine;

public class AudioCuePlayer : MonoBehaviour
{
    public static int Play(AudioCue cue, AudioSource audioSource, int _previousClip = -1)
    {
        if (!cue || !audioSource || !audioSource.isActiveAndEnabled)
            return -1;

        if (cue.clips.Length < 1)
            return -1;

        float vol = UnityEngine.Random.Range(cue.minVolume, cue.maxVolume) * cue.volumeMultiplier;
        int clipIndex;

        if (!cue.dontAdjustVolume)
            audioSource.volume = vol;
        else vol = audioSource.volume;

        if (cue.forcedPitch == 1)
            audioSource.pitch = UnityEngine.Random.Range(cue.minPitch, cue.maxPitch);
        else audioSource.pitch = cue.forcedPitch;

        clipIndex = UnityEngine.Random.Range(0, cue.clips.Length);

        if (clipIndex == _previousClip)
            clipIndex++;

        if (clipIndex >= cue.clips.Length)
            clipIndex = 0;

        if (cue.clips[clipIndex] == null)
            return -1;

        if (cue.loop)
        {
            if (cue.stopPrevious)
                audioSource.Stop();

            audioSource.loop = true;
            audioSource.clip = cue.clips[clipIndex];
            audioSource.Play();
        }
        else
        {
            audioSource.loop = false;
            audioSource.PlayOneShot(cue.clips[clipIndex]);

            if (cue.overlayClips.Length > 0)
                audioSource.PlayOneShot(cue.overlayClips[UnityEngine.Random.Range(0, cue.overlayClips.Length)]);
        }
        return clipIndex;
    }

    public static void Stop(AudioCue cue, AudioSource audioSource)
    {
        if (audioSource == null || !audioSource.isActiveAndEnabled)
        {
            Debug.LogWarning("AudioSource is null or not active. Cannot stop sound.");
            return;
        }
        audioSource.loop = false;
        audioSource.Stop();
    }

    public static bool IsPlaying(AudioSource audioSource)
    {
        return audioSource.isPlaying;
    }
}
