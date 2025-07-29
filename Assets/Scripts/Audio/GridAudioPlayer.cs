using System;
using System.Collections;
using UnityEngine;

public class GridAudioPlayer : MonoBehaviour
{
    [SerializeField] private AudioCue tileMoveAudio;
    [SerializeField] private AudioCue tileHitAudio;
    [SerializeField] private AudioCue tileMatchAudio;
    [SerializeField] private AudioCue tileSwitchErrorAudio;
    [SerializeField] private AudioCue tileDestroyAudio;
    [SerializeField] private AudioCue tileLineDestroyer;
    [SerializeField] private AudioCue tileBomb;
    [SerializeField] private AudioCue tileRainbow;
    [SerializeField] private AudioCue powerTileCreation;

    private AudioSource audioSource;
    private ISoundManager soundManager;

    private IEnumerator Start()
    {
        yield return new WaitUntil(() => Services.Get<IGameSessionService>().IsLevelDataLoaded);

        audioSource = GetComponent<AudioSource>();
        soundManager = Services.Get<ISoundManager>();

        GridController.Instance.TileDrop += PlayHitAudio;
        GridController.Instance.TileSwapped += PlayMatchAudio;
        GridController.Instance.TileSwapError += PlaySwitchErrorAudio;
        GridController.Instance.TileDestroyed += PlayDestroyAudio;
        GridController.Instance.TileMoved += PlayTileMoveAudio;
        GridController.Instance.PowerTileCreated += PlayPowerTileCreationAudio;
        GridController.Instance.GridContext.OnSpecialTileTriggered += PlaySpecialTileAudio;
    }

    private void PlaySpecialTileAudio(TileData data)
    {
        switch (data.Power)
        {
            case TilePower.ColumnClearer:
                soundManager.Play(tileLineDestroyer, audioSource);
                break;
            case TilePower.RowClearer:
                soundManager.Play(tileLineDestroyer, audioSource);
                break;
            case TilePower.Bomb:
                soundManager.Play(tileBomb, audioSource);
                break;
            case TilePower.Rainbow:
                soundManager.Play(tileRainbow, audioSource);
                break;
            default:
                break;
        }
    }

    private void PlayPowerTileCreationAudio()
    {
        soundManager.Play(powerTileCreation, audioSource);
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
