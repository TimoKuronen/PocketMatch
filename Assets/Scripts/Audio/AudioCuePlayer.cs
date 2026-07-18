using UnityEngine;

public static class AudioCuePlayer
{
    public static int Play(AudioCue cue, AudioSource audioSource, int previousClip = -1, float volumeScale = 1f)
    {
        if (!TryPrepare(cue, audioSource, previousClip, volumeScale, out int clipIndex, out AudioClip clip))
            return -1;

        bool useDedicatedVoice = cue.loop || cue.stopPrevious;

        if (useDedicatedVoice)
        {
            audioSource.Stop();
            audioSource.loop = cue.loop;
            audioSource.clip = clip;
            audioSource.Play();
        }
        else
        {
            audioSource.loop = false;
            audioSource.PlayOneShot(clip);

            if (cue.overlayClips != null && cue.overlayClips.Length > 0)
            {
                var overlay = cue.overlayClips[Random.Range(0, cue.overlayClips.Length)];
                if (overlay != null)
                    audioSource.PlayOneShot(overlay);
            }
        }

        return clipIndex;
    }

    /// <summary>
    /// Plays a cue on a dedicated voice: always stops the source first so sounds do not stack.
    /// </summary>
    public static int PlayExclusive(AudioCue cue, AudioSource audioSource, float volumeScale = 1f)
    {
        if (!TryPrepare(cue, audioSource, -1, volumeScale, out int clipIndex, out AudioClip clip))
            return -1;

        audioSource.Stop();
        audioSource.loop = false;
        audioSource.clip = clip;
        audioSource.Play();
        return clipIndex;
    }

    public static void Stop(AudioSource audioSource)
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
        return audioSource != null && audioSource.isPlaying;
    }

    public static float GetPlayDuration(AudioCue cue, int clipIndex)
    {
        if (cue == null)
            return 0f;

        if (clipIndex >= 0 && cue.clips != null && clipIndex < cue.clips.Length && cue.clips[clipIndex] != null)
            return cue.clips[clipIndex].length;

        if (cue.playDuration > 0f)
            return cue.playDuration;

        return 0.5f;
    }

    private static bool TryPrepare(
        AudioCue cue,
        AudioSource audioSource,
        int previousClip,
        float volumeScale,
        out int clipIndex,
        out AudioClip clip)
    {
        clipIndex = -1;
        clip = null;

        if (!cue || !audioSource || !audioSource.isActiveAndEnabled)
            return false;

        if (cue.clips == null || cue.clips.Length < 1)
            return false;

        float vol = Random.Range(cue.minVolume, cue.maxVolume) * cue.volumeMultiplier * Mathf.Clamp01(volumeScale);
        if (!cue.dontAdjustVolume)
            audioSource.volume = vol;

        if (Mathf.Approximately(cue.forcedPitch, 1f))
            audioSource.pitch = Random.Range(cue.minPitch, cue.maxPitch);
        else
            audioSource.pitch = cue.forcedPitch;

        clipIndex = PickClipIndex(cue, previousClip);
        clip = cue.clips[clipIndex];
        return clip != null;
    }

    private static int PickClipIndex(AudioCue cue, int previousClip)
    {
        if (!cue.randomize)
            return 0;

        if (cue.clips.Length == 1)
            return 0;

        int clipIndex = Random.Range(0, cue.clips.Length);
        if (clipIndex == previousClip)
            clipIndex = (clipIndex + 1) % cue.clips.Length;

        return clipIndex;
    }
}
