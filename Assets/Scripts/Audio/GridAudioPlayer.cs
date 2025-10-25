using System.Collections;
using UnityEngine;

public class GridAudioPlayer : MonoBehaviour
{
    [SerializeField] private AudioCue tileMoveAudio;
    [SerializeField] private AudioCue tileHitAudio;
    [SerializeField] private AudioCue tileMatchAudio;
    [SerializeField] private AudioCue tileSwitchErrorAudio;
    [SerializeField] private AudioCue tileDestroyAudio;
    [SerializeField] private AudioCue tileLineDestroyerAudio;
    [SerializeField] private AudioCue tileBombAudio;
    [SerializeField] private AudioCue tileRainbowAudio;
    [SerializeField] private AudioCue powerTileCreationAudio;
    [SerializeField] private AudioCue levelWonAudio;
    [SerializeField] private AudioCue levelLostAudio;

    private AudioSource audioSource;
    private ISoundManager soundManager;
    private ILevelManager levelManager;

    private IEnumerator Start()
    {
        yield return new WaitUntil(() => GridController.Instance.IsBoardInitialized);

        audioSource = GetComponent<AudioSource>();
        soundManager = Services.Get<ISoundManager>();
        levelManager = Services.Get<ILevelManager>();

        GridController.Instance.TileDrop += PlayHitAudio;
        GridController.Instance.TileSwapped += PlayMatchAudio;
        GridController.Instance.TileSwapError += PlaySwitchErrorAudio;
        GridController.Instance.TileDestroyed += PlayDestroyAudio;
        GridController.Instance.TileMoved += PlayTileMoveAudio;
        GridController.Instance.PowerTileCreated += PlayPowerTileCreationAudio;
        GridController.Instance.GridContext.OnSpecialTileTriggered += PlaySpecialTileAudio;
        levelManager.OnLevelWon += PlayLevelWonAudio;
        levelManager.OnLevelLost += PlayLevelLostAudio;
    }

    private void PlaySpecialTileAudio(TileData data)
    {
        switch (data.Power)
        {
            case TilePower.ColumnClearer:
                soundManager.Play(tileLineDestroyerAudio, audioSource);
                break;
            case TilePower.RowClearer:
                soundManager.Play(tileLineDestroyerAudio, audioSource);
                break;
            case TilePower.Bomb:
                soundManager.Play(tileBombAudio, audioSource);
                break;
            case TilePower.Rainbow:
                soundManager.Play(tileRainbowAudio, audioSource);
                break;
            default:
                break;
        }
    }

    private void PlayPowerTileCreationAudio(TileData tileData)
    {
        soundManager.Play(powerTileCreationAudio, audioSource);
    }


    private void PlayTileMoveAudio()
    {
        soundManager.Play(tileMoveAudio, audioSource);
    }

    private void PlayDestroyAudio(TileData data)
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

    private void PlayLevelLostAudio()
    {
        soundManager.Play(levelLostAudio, audioSource);
    }

    private void PlayLevelWonAudio()
    {
        soundManager.Play(levelWonAudio, audioSource);
    }

    private void OnDestroy()
    {
        GridController.Instance.TileDrop -= PlayHitAudio;
        GridController.Instance.TileSwapped -= PlayMatchAudio;
        GridController.Instance.TileSwapError -= PlaySwitchErrorAudio;
        GridController.Instance.TileDestroyed -= PlayDestroyAudio;
        GridController.Instance.TileMoved -= PlayTileMoveAudio;
        GridController.Instance.PowerTileCreated -= PlayPowerTileCreationAudio;
        GridController.Instance.GridContext.OnSpecialTileTriggered -= PlaySpecialTileAudio;
        levelManager.OnLevelWon -= PlayLevelWonAudio;
        levelManager.OnLevelLost -= PlayLevelLostAudio;
    }
}
