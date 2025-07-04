using System;
using UnityEngine;

public class GridAudioPlayer : MonoBehaviour
{
    [SerializeField] private AudioCue tileMoveAudio;
    [SerializeField] private AudioCue tileHitAudio;
    [SerializeField] private AudioCue tileMatchAudio;
    [SerializeField] private AudioCue tileSwitchErrorAudio;
    [SerializeField] private AudioCue tileDestroyAudio;

    private AudioSource audioSource;
    private ISoundManager soundManager;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        soundManager = Services.Get<ISoundManager>();

        GridController.Instance.TileDrop += PlayHitAudio;
        GridController.Instance.TileSwapped += PlayMatchAudio;
        GridController.Instance.TileSwapError += PlaySwitchErrorAudio;
        GridController.Instance.TileDestroyed += PlayDestroyAudio;
        GridController.Instance.TileMoved += PlayTileMoveAudio;
    }

    private void PlayTileMoveAudio()
    {
        soundManager.Play(tileMoveAudio, audioSource);
    }

    private void PlayDestroyAudio()
    {
        soundManager.Play(tileDestroyAudio, audioSource);
    }

    private void PlaySwitchErrorAudio()
    {
        soundManager.Play(tileSwitchErrorAudio, audioSource);
    }

    private void PlayMatchAudio()
    {
        soundManager.Play(tileMatchAudio, audioSource);
    }

    private void PlayHitAudio()
    {
        soundManager.Play(tileHitAudio, audioSource);
    }

    private void OnDestroy()
    {
        GridController.Instance.TileDrop -= PlayHitAudio;
        GridController.Instance.TileSwapped -= PlayMatchAudio;
        GridController.Instance.TileSwapError -= PlaySwitchErrorAudio;
        GridController.Instance.TileDestroyed -= PlayDestroyAudio;
    }
}
