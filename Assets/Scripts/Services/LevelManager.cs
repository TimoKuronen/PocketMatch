using System;
using System.Collections;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class LevelManager : ILevelManager, IDisposable, IStartable
{
    public int MovesRemaining { get; private set; }
    public MapData LocalMapData { get; private set; }
    public VictoryConditions VictoryConditions { get; private set; }
    public Action OnVictoryConditionsUpdated { get; set; }
    public Action OnLevelWon { get; set; }
    public Action OnLevelLost { get; set; }
    public int GameTimeInSeconds { get; private set; }

    private IGameSessionService gameSessionService;
    private IGridController gridController;
    private IScoreService scoreService;

    [Inject]
    public void Construct(
        IGameSessionService gameSessionService,
        IGridController gridController,
        IScoreService scoreService)
    {
        this.gameSessionService = gameSessionService;
        this.gridController = gridController;
        this.scoreService = scoreService;
    }

    public void Start()
    {
        // Start game timer immediately (doesn't depend on session)
        TaskRunner.Instance.StartCoroutine(GameTimer());

        // Subscribe to session loaded event instead of polling
        GameSignals.OnSessionLoaded += OnSessionLoaded;
        
        // If session is already loaded, initialize immediately
        if (GameSignals.IsSessionLoaded)
        {
            OnSessionLoaded();
        }
    }

    private void OnSessionLoaded()
    {
        LocalMapData = MonoBehaviour.Instantiate(gameSessionService.CurrentMapData);

        if (LocalMapData == null)
        {
            Debug.LogError("MapData not assigned.");
            return;
        }

        MovesRemaining = LocalMapData.VictoryConditions.MoveLimit;

        Debug.Log($"LevelManager {LocalMapData.name} initialized with MoveLimit: {MovesRemaining}");
        VictoryConditions = LocalMapData.VictoryConditions;

        // Wait for grid controller to be initialized before subscribing to events
        TaskRunner.Instance.StartCoroutine(WaitForGridInitialization());
    }

    private IEnumerator WaitForGridInitialization()
    {
        yield return new WaitUntil(() => gridController != null && gridController.IsBoardInitialized);

        SubscribeToEvents();

        // Raise level started event - AnalyticsService and ScoreService will listen to this
        // Level index will be retrieved by event handlers from SaveService
        LevelEvents.RaiseLevelStarted(new LevelStartedEventArgs(
            gameSessionService.CurrentMapData.name,
            0, // Level index will be retrieved by event handlers
            LocalMapData.VictoryConditions.MoveLimit));
    }

    private IEnumerator GameTimer()
    {
        GameTimeInSeconds = 0;

        while (true)
        {
            yield return CachedCoroutines.Wait(1f);
            GameTimeInSeconds++;
        }
    }

    private void SubscribeToEvents()
    {
        gridController.ActionTaken += OnActionTaken;
        gridController.BoardUpdated += CheckVictoryConditions;
        gridController.TileDestroyed += OnTileDestroyed;
        gridController.GridContext.OnDestroy += OnTileDestroyed;

        UIGameHUD.OnCheatButtonClicked += ToggleWinEvent;
    }

    private void OnTileDestroyed(TileData data)
    {
        if (data.State == TileState.Destroyable)
        {
            VictoryConditions.DestroyableTileCount--;
            Debug.Log("Destroyable tile destroyed, decrementing count to " + VictoryConditions.DestroyableTileCount);
        }
        else if (VictoryConditions.RequiredColorMatchCount != null && VictoryConditions.RequiredColorMatchCount.Length > 0)
        {
            foreach (var match in VictoryConditions.RequiredColorMatchCount)
            {
                if (data.Type == match.TileColor)
                {
                    match.TileCount--;

                    if (match.TileCount < 0)
                        match.TileCount = 0;
                }
            }
        }

        OnVictoryConditionsUpdated?.Invoke();
    }

    private void CheckVictoryConditions(TileData[,] obj)
    {
        if (!AreVictoryConditionsMet())
        {
            if (MovesRemaining <= 0)
            {
                ToggleLoseEvent();
            }
        }
        else
        {
            ToggleWinEvent();
        }
    }

    private bool AreVictoryConditionsMet()
    {
        // Check if all required colors have been matched
        if (VictoryConditions.RequiredColorMatchCount != null && VictoryConditions.RequiredColorMatchCount.Length > 0)
        {
            foreach (var match in VictoryConditions.RequiredColorMatchCount)
            {
                if (match.TileCount > 0)
                {
                    //Debug.Log($"Victory conditions to destroy colors, still required : {match.TileCount} ");
                    return false;
                }
                else Debug.Log($"Victory condition met for color: {match.TileColor}");
            }
        }
        // Check if all the required destroyable tiles have been cleared
        if (VictoryConditions.DestroyableTileCount > 0)
        {
            //Debug.Log("Victory condition not met: Destroyable tiles remaining " + VictoryConditions.DestroyableTileCount);
            return false;
        }
        else Debug.Log("All destroyable tiles cleared.");

        return true;
    }

    private void OnActionTaken()
    {
        MovesRemaining--;
        OnVictoryConditionsUpdated?.Invoke();
    }

    private void ToggleWinEvent()
    {
        int movesSpent = LocalMapData.VictoryConditions.MoveLimit - MovesRemaining;
        int totalScore = scoreService.GetTotalScore();
        
        // Raise level completed event - SaveService and AnalyticsService will listen to this
        LevelEvents.RaiseLevelCompleted(new LevelCompletedEventArgs(
            LocalMapData.name,
            MovesRemaining,
            movesSpent,
            totalScore,
            GameTimeInSeconds,
            gameSessionService.IsLevelCapReached));

        OnLevelWon?.Invoke();
    }

    private void ToggleLoseEvent()
    {
        // Raise level failed event - AnalyticsService will listen to this
        LevelEvents.RaiseLevelFailed(new LevelFailedEventArgs(
            LocalMapData.name,
            GameTimeInSeconds));

        OnLevelLost?.Invoke();
    }

    public void Dispose()
    {
        GameSignals.OnSessionLoaded -= OnSessionLoaded;

        if (gridController != null)
        {
            gridController.ActionTaken -= OnActionTaken;
            gridController.BoardUpdated -= CheckVictoryConditions;
            gridController.TileDestroyed -= OnTileDestroyed;
            gridController.GridContext.OnDestroy -= OnTileDestroyed;
        }

        UIGameHUD.OnCheatButtonClicked -= ToggleWinEvent;
    }
}