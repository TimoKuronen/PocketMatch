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
    private AudioSource hitAudioSource;
    private IAudioService audioService;
    private ILevelManager levelManager;
    private IGridController gridController;

    [Inject]
    public void Construct(IAudioService audioService, ILevelManager levelManager, IGridController gridController)
    {
        this.audioService = audioService;
        this.levelManager = levelManager;
        this.gridController = gridController;
    }

    public void Start()
    {
        audioSource = GetComponent<AudioSource>();
        hitAudioSource = gameObject.AddComponent<AudioSource>();
        hitAudioSource.playOnAwake = false;

        // Subscribe to board initialization event instead of polling
        if (gridController != null)
        {
            gridController.BoardUpdated += OnBoardInitialized;
            
            // If board is already initialized, subscribe to events immediately
            if (gridController.IsBoardInitialized)
            {
                OnBoardInitialized(null);
            }
        }

        // Level manager events can be subscribed immediately
        if (levelManager != null)
        {
            levelManager.OnLevelWon += PlayLevelWonAudio;
            levelManager.OnLevelLost += PlayLevelLostAudio;
        }
    }

    private void OnBoardInitialized(TileData[,] boardData)
    {
        // Unsubscribe from initialization event
        gridController.BoardUpdated -= OnBoardInitialized;

        // Subscribe to grid controller events
        gridController.TileDrop += PlayHitAudio;
        gridController.TileSwapped += PlayMatchAudio;
        gridController.TileSwapError += PlaySwitchErrorAudio;
        gridController.TilesDestroyed += PlayDestroyAudio;
        gridController.TileMoved += PlayTileMoveAudio;
        gridController.PowerTileCreated += PlayPowerTileCreationAudio;
        gridController.GridContext.OnSpecialTileTriggered += PlaySpecialTileAudio;
        gridController.OnBoardShuffle += PlayShuffleAudio;
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

    private void PlayDestroyAudio()
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
        audioService.PlayExclusive(tileHitAudio, hitAudioSource);
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
        if (gridController != null)
        {
            gridController.TileDrop -= PlayHitAudio;
            gridController.TileSwapped -= PlayMatchAudio;
            gridController.TileSwapError -= PlaySwitchErrorAudio;
            gridController.TilesDestroyed -= PlayDestroyAudio;
            gridController.TileMoved -= PlayTileMoveAudio;
            gridController.PowerTileCreated -= PlayPowerTileCreationAudio;
            gridController.GridContext.OnSpecialTileTriggered -= PlaySpecialTileAudio;
            gridController.OnBoardShuffle -= PlayShuffleAudio;
        }

        if (levelManager != null)
        {
            levelManager.OnLevelWon -= PlayLevelWonAudio;
            levelManager.OnLevelLost -= PlayLevelLostAudio;
        }
    }
}
