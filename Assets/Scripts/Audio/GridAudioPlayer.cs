using System.Collections;
using UnityEngine;
using VContainer;

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
    [SerializeField] private AudioCue shuffleAudio;

    private AudioSource audioSource;
    private IAudioService audioService;
    private ILevelManager levelManager;

    [Inject]
    public void Construct(IAudioService audioService, ILevelManager levelManager)
    {
        this.audioService = audioService;
        this.levelManager = levelManager;
    }

    private IEnumerator Start()
    {
        yield return new WaitUntil(() => GridController.Instance.IsBoardInitialized);

        audioSource = GetComponent<AudioSource>();

        GridController.Instance.TileDrop += PlayHitAudio;
        GridController.Instance.TileSwapped += PlayMatchAudio;
        GridController.Instance.TileSwapError += PlaySwitchErrorAudio;
        GridController.Instance.TileDestroyed += PlayDestroyAudio;
        GridController.Instance.TileMoved += PlayTileMoveAudio;
        GridController.Instance.PowerTileCreated += PlayPowerTileCreationAudio;
        GridController.Instance.GridContext.OnSpecialTileTriggered += PlaySpecialTileAudio;
        GridController.Instance.OnBoardShuffle += PlayShuffleAudio;

        levelManager.OnLevelWon += PlayLevelWonAudio;
        levelManager.OnLevelLost += PlayLevelLostAudio;
    }

    private void PlaySpecialTileAudio(TileData data)
    {
        switch (data.Power)
        {
            case TilePower.ColumnClearer:
                audioService.Play(tileLineDestroyerAudio, audioSource);
                break;
            case TilePower.RowClearer:
                audioService.Play(tileLineDestroyerAudio, audioSource);
                break;
            case TilePower.Bomb:
                audioService.Play(tileBombAudio, audioSource);
                break;
            case TilePower.Rainbow:
                audioService.Play(tileRainbowAudio, audioSource);
                break;
            default:
                break;
        }
    }

    private void PlayPowerTileCreationAudio(TileData tileData)
    {
        audioService.Play(powerTileCreationAudio, audioSource);
    }

    private void PlayTileMoveAudio()
    {
        audioService.Play(tileMoveAudio, audioSource);
    }

    private void PlayDestroyAudio(TileData data)
    {
        audioService.Play(tileDestroyAudio, audioSource);
    }

    private void PlaySwitchErrorAudio()
    {
        audioService.Play(tileSwitchErrorAudio, audioSource);
    }

    private void PlayMatchAudio()
    {
        audioService.Play(tileMatchAudio, audioSource);
    }

    private void PlayHitAudio()
    {
        audioService.Play(tileHitAudio, audioSource);
    }

    private void PlayLevelLostAudio()
    {
        audioService.Play(levelLostAudio, audioSource);
    }

    private void PlayLevelWonAudio()
    {
        audioService.Play(levelWonAudio, audioSource);
    }

    private void PlayShuffleAudio()
    {
        audioService.Play(shuffleAudio, audioSource);
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
        GridController.Instance.OnBoardShuffle -= PlayShuffleAudio;

        levelManager.OnLevelWon -= PlayLevelWonAudio;
        levelManager.OnLevelLost -= PlayLevelLostAudio;
    }
}
