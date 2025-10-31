using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

public class LevelManager : ILevelManager, IDisposable, ITickable
{
    public static int MovesRemaining { get; private set; }
    public MapData LocalMapData { get; private set; }
    public VictoryConditions VictoryConditions { get; private set; }
    public Action<LevelManager> OnVictoryConditionsUpdated { get; set; }
    public Action OnLevelWon { get; set; }
    public Action OnLevelLost { get; set; }
    public int GameTimeInSeconds { get; private set; }

    private ISaveService saveService;
    private IGameSessionService gameSessionService;
    private IAnalyticsService analyticsService;
    private IScoreService scoreService;

    private bool gameInProgress = true;

    [Inject]
    public void Construct(
        ISaveService saveService, 
        IGameSessionService gameSessionService, 
        IAnalyticsService analyticsService,
        IScoreService scoreService)
    {
        this.saveService = saveService;
        this.gameSessionService = gameSessionService;
        this.analyticsService = analyticsService;
        this.scoreService = scoreService;

        CoroutineMonoBehavior.Instance.StartCoroutine(SetLevelData());
        CoroutineMonoBehavior.Instance.StartCoroutine(GameTimer());
    }

    private IEnumerator SetLevelData()
    {
        yield return new WaitUntil(() => GameSignals.IsSessionLoaded);

        LocalMapData = MonoBehaviour.Instantiate(gameSessionService.CurrentMapData);

        if (LocalMapData == null)
        {
            Debug.LogError("MapData not assigned.");
            yield break;
        }

        MovesRemaining = LocalMapData.VictoryConditions.MoveLimit;

        Debug.Log($"LevelManager {LocalMapData.name} initialized with MoveLimit: {MovesRemaining}");
        VictoryConditions = LocalMapData.VictoryConditions;

        yield return new WaitUntil(() => GridController.Instance != null && GridController.Instance.IsBoardInitialized);

        SubscribeToEvents();

        analyticsService.LogEvent(AnalyticsEvents.LevelStarted, new System.Collections.Generic.Dictionary<string, object>
        {
            { "level_name", gameSessionService.CurrentMapData.name },
            { "level_index", saveService.PlayerData.nextLevelIndex + 1 }
        });
    }

    private IEnumerator GameTimer()
    {
        GameTimeInSeconds = 0;

        while (true)
        {
            yield return new WaitForSeconds(1f);
            GameTimeInSeconds++;
        }
    }

    private void SubscribeToEvents()
    {
        GridController.Instance.ActionTaken += OnActionTaken;
        GridController.Instance.BoardUpdated += CheckVictoryConditions;
        GridController.Instance.TileDestroyed += OnTileDestroyed;
        GridController.Instance.GridContext.OnDestroy += OnTileDestroyed;
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

        OnVictoryConditionsUpdated?.Invoke(this);
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
        OnVictoryConditionsUpdated?.Invoke(this);
    }

    private void ToggleWinEvent()
    {
        if (gameSessionService.IsLevelCapReached)
        {
            Debug.Log("Level cap reached, not incrementing level index.");
            OnLevelWon?.Invoke();
            return;
        }

        gameInProgress = false;

        saveService.PlayerData.nextLevelIndex++;
        saveService.PlayerData.coins += scoreService.GetTotalScore();
        saveService.Save();

        analyticsService.LogEvent(AnalyticsEvents.LevelCompleted, new System.Collections.Generic.Dictionary<string, object>
        {
            { "level_name", LocalMapData.name },
            { "moves_spent", LocalMapData.VictoryConditions.MoveLimit - MovesRemaining },
            { "total_score", scoreService.GetTotalScore() },
            { "matchDuration", GameTimeInSeconds }
        });

        OnLevelWon?.Invoke();
    }

    private void ToggleLoseEvent()
    {
        gameInProgress = false;

        analyticsService.LogEvent(AnalyticsEvents.LevelFailed, new System.Collections.Generic.Dictionary<string, object>
        {
            { "level_name", LocalMapData.name },
            { "matchDuration", GameTimeInSeconds }
        });

        OnLevelLost?.Invoke();
    }

    public void Dispose()
    {
        GridController.Instance.ActionTaken -= OnActionTaken;
        GridController.Instance.BoardUpdated -= CheckVictoryConditions;
        GridController.Instance.TileDestroyed -= OnTileDestroyed;
        GridController.Instance.GridContext.OnDestroy -= OnTileDestroyed;
    }

    public void Tick()
    {
#if UNITY_EDITOR
        if (Keyboard.current.wKey.wasPressedThisFrame)
        {
            ToggleWinEvent();
        }
        if (Keyboard.current.lKey.wasPressedThisFrame)
        {
            ToggleLoseEvent();
        }
#endif
    }
}