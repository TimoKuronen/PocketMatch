using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VContainer;
using Cysharp.Threading.Tasks;

public class AudioService : IAudioService, IDisposable
{
    private int maxSimultaneousUISounds = 5;

    private Dictionary<SoundType, List<PlayingSound>> activeSounds;

    [Inject]
    public void Construct()
    {
        activeSounds = new Dictionary<SoundType, List<PlayingSound>>();
    }

    public void Play(AudioCue data, AudioSource audioSource)
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

        if (!activeSounds.TryGetValue(data.soundType, out List<PlayingSound> soundList))
        {
            soundList = new List<PlayingSound>();
            activeSounds[data.soundType] = soundList;
        }
        int maxSounds = GetMaxSimultaneousSounds(data.soundType);

        float now = Time.time;
        soundList.RemoveAll(s => now - s.StartTime > s.Cue.playDuration);

        if (soundList.Count >= maxSounds)
        {
            var oldest = soundList.OrderBy(s => s.StartTime).FirstOrDefault();
            if (oldest != null)
            {
                //Debug.Log("Stopping oldest sound because count is " + soundList.Count + " and max sound count is " + maxSounds);
                AudioCuePlayer.Stop(oldest.Cue, oldest.Source);
                soundList.Remove(oldest);
            }
        }

        PlaySoundAsync(audioSource, data).Forget();
        soundList.Add(new PlayingSound
        {
            Cue = data,
            Source = audioSource,
            StartTime = Time.time
        });
    }

    private async UniTask PlaySoundAsync(AudioSource audioSource, AudioCue audioCue)
    {
        if (audioSource == null)
        {
            Debug.LogWarning("AudioSource is NULL. Cannot play sound.");
            return;
        }

        AudioCuePlayer.Play(audioCue, audioSource);

        await UniTask.Delay(TimeSpan.FromSeconds(audioCue.playDuration));

        if (activeSounds.TryGetValue(audioCue.soundType, out List<PlayingSound> soundList))
        {
            var entry = soundList.FirstOrDefault(s => s.Cue == audioCue && s.Source == audioSource);
            if (entry != null)
                soundList.Remove(entry);
        }
    }

    private int GetMaxSimultaneousSounds(SoundType soundType)
    {
        return soundType switch
        {
            SoundType.UI => maxSimultaneousUISounds,
            _ => maxSimultaneousUISounds,
        };
    }

    public void Dispose()
    {
        foreach (var list in activeSounds.Values)
        {
            foreach (var sound in list)
            {
                AudioCuePlayer.Stop(sound.Cue, sound.Source);
            }
            list.Clear();
        }
    }

    private class PlayingSound
    {
        public AudioCue Cue;
        public AudioSource Source;
        public float StartTime;
    }
}

public enum SoundType { UI, Other }