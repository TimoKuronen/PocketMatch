using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SoundManager : ISoundManager
{
    private int maxSimultaneousUISounds = 10;

    private Dictionary<SoundType, List<PlayingSound>> activeSounds;

    public void Initialize()
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

        if (!Enum.IsDefined(typeof(SoundType), data.soundType))
        {
            Debug.LogWarning($"Invalid SoundType: {data.soundType}");
            return;
        }

        if (!activeSounds.ContainsKey(data.soundType))
        {
            activeSounds[data.soundType] = new List<PlayingSound>();
        }

        List<PlayingSound> soundList = activeSounds[data.soundType];
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

        CoroutineMonoBehavior.Instance.StartCoroutine(PlaySoundCoroutine(audioSource, data));
        soundList.Add(new PlayingSound
        {
            Cue = data,
            Source = audioSource,
            StartTime = Time.time
        });
    }

    private IEnumerator PlaySoundCoroutine(AudioSource audioSource, AudioCue audioCue)
    {
        if (audioSource == null)
        {
            Debug.LogWarning("AudioSource is NULL. Cannot play sound.");
            yield break;
        }

        AudioCuePlayer.Play(audioCue, audioSource);

        yield return new WaitForSeconds(audioCue.playDuration);

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
            _ => 1,
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