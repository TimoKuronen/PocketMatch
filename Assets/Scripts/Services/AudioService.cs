using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VContainer;
using Cysharp.Threading.Tasks;

public class AudioService : IAudioService, IDisposable
{
    private const string SfxVolumePrefKey = "SfxVolume";
    private const int MaxSimultaneousUISounds = 5;

    private Dictionary<SoundType, List<PlayingSound>> activeSounds;
    private float sfxVolume = 1f;

    public float SfxVolume
    {
        get => sfxVolume;
        set
        {
            sfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SfxVolumePrefKey, sfxVolume);
            PlayerPrefs.Save();
        }
    }

    [Inject]
    public void Construct()
    {
        activeSounds = new Dictionary<SoundType, List<PlayingSound>>();
        sfxVolume = PlayerPrefs.GetFloat(SfxVolumePrefKey, 1f);
    }

    public void Play(AudioCue data, AudioSource audioSource)
    {
        PlayInternal(data, audioSource, exclusive: false);
    }

    public void PlayExclusive(AudioCue data, AudioSource audioSource)
    {
        PlayInternal(data, audioSource, exclusive: true);
    }

    private void PlayInternal(AudioCue data, AudioSource audioSource, bool exclusive)
    {
        if (data == null)
        {
            Debug.LogWarning("AudioCue data is NULL! Cannot play sound.");
            return;
        }

        if (data.soundType != SoundType.UI && data.soundType != SoundType.Other)
        {
            Debug.LogWarning($"Invalid SoundType: {data.soundType}");
            return;
        }

        if (audioSource == null)
        {
            Debug.LogWarning("AudioSource is NULL. Cannot play sound.");
            return;
        }

        if (!activeSounds.TryGetValue(data.soundType, out List<PlayingSound> soundList))
        {
            soundList = new List<PlayingSound>();
            activeSounds[data.soundType] = soundList;
        }

        float now = Time.time;
        soundList.RemoveAll(s => now - s.StartTime > s.Duration);

        if (soundList.Count >= MaxSimultaneousUISounds)
        {
            var oldest = soundList.OrderBy(s => s.StartTime).FirstOrDefault();
            if (oldest != null)
            {
                AudioCuePlayer.Stop(oldest.Source);
                soundList.Remove(oldest);
            }
        }

        int clipIndex = exclusive
            ? AudioCuePlayer.PlayExclusive(data, audioSource, sfxVolume)
            : AudioCuePlayer.Play(data, audioSource, volumeScale: sfxVolume);

        if (clipIndex < 0)
            return;

        float duration = AudioCuePlayer.GetPlayDuration(data, clipIndex);
        soundList.Add(new PlayingSound
        {
            Cue = data,
            Source = audioSource,
            StartTime = Time.time,
            Duration = duration
        });

        ClearAfterDurationAsync(data, audioSource, duration).Forget();
    }

    private async UniTaskVoid ClearAfterDurationAsync(AudioCue audioCue, AudioSource audioSource, float duration)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(duration));

        if (activeSounds.TryGetValue(audioCue.soundType, out List<PlayingSound> soundList))
        {
            var entry = soundList.FirstOrDefault(s => s.Cue == audioCue && s.Source == audioSource);
            if (entry != null)
                soundList.Remove(entry);
        }
    }

    public void Dispose()
    {
        foreach (var list in activeSounds.Values)
        {
            foreach (var sound in list)
            {
                AudioCuePlayer.Stop(sound.Source);
            }
            list.Clear();
        }
    }

    private class PlayingSound
    {
        public AudioCue Cue;
        public AudioSource Source;
        public float StartTime;
        public float Duration;
    }
}

public enum SoundType { UI, Other }
