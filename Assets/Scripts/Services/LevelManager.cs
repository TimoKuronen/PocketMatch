using System;
using System.Collections;
using UnityEngine;

public class LevelManager : ILevelManager
{
    public int MovesRemaining { get; private set; }
    public MapData LocalMapData { get; private set; }
    public VictoryConditions VictoryConditions { get; private set; }
    public Action<LevelManager> VictoryConditionsUpdated { get; set; }
    public Action LevelWon { get; set; }
    public Action LevelLost { get; set; }
    public int GameTimeInSeconds { get; private set; }

    private ISaveService saveService;
    private bool gameInProgress = true;

    public void Initialize()
    {
        CoroutineMonoBehavior.Instance.StartCoroutine(SetLevelData());
        CoroutineMonoBehavior.Instance.StartCoroutine(GameTimer());

        saveService = Services.Get<ISaveService>();
    }

    private IEnumerator SetLevelData()
    {
        yield return new WaitUntil(() => Services.Get<IGameSessionService>().IsLevelDataLoaded);

        LocalMapData = MonoBehaviour.Instantiate(Services.Get<IGameSessionService>().CurrentMapData);

        if (LocalMapData == null)
        {
            Debug.LogError("MapData not assigned.");
            yield break;
        }

        MovesRemaining = LocalMapData.VictoryConditions.MoveLimit;

        Debug.Log($"LevelManager {LocalMapData.name} initialized with MoveLimit: {MovesRemaining}");
        VictoryConditions = LocalMapData.VictoryConditions;

        yield return new WaitUntil(() => GridController.Instance != null);

        SubscribeToEvents();

        Services.Get<IAnalyticsManager>().LogEvent(AnalyticsEvents.LevelStarted, new System.Collections.Generic.Dictionary<string, object>
        {
            { "level_name", Services.Get<IGameSessionService>().CurrentMapData.name },
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

        VictoryConditionsUpdated?.Invoke(this);
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
        VictoryConditionsUpdated?.Invoke(this);
    }

    private void ToggleWinEvent()
    {
        if (Services.Get<IGameSessionService>().LevelCapReached)
        {
            Debug.Log("Level cap reached, not incrementing level index.");
            LevelWon?.Invoke();
            return;
        }

        gameInProgress = false;

        saveService.PlayerData.nextLevelIndex++;
        saveService.PlayerData.coins += Services.Get<IScoreManager>().GetTotalScore();
        saveService.Save();

        Services.Get<IAnalyticsManager>().LogEvent(AnalyticsEvents.LevelCompleted, new System.Collections.Generic.Dictionary<string, object>
        {
            { "level_name", LocalMapData.name },
            { "moves_spent", LocalMapData.VictoryConditions.MoveLimit - MovesRemaining },
            { "total_score", Services.Get<IScoreManager>().GetTotalScore() },
            { "moves_spent", LocalMapData.VictoryConditions.MoveLimit - MovesRemaining },
            { "matchDuration", GameTimeInSeconds }
        });

        LevelWon?.Invoke();
    }

    private void ToggleLoseEvent()
    {
        gameInProgress = false;

        Services.Get<IAnalyticsManager>().LogEvent(AnalyticsEvents.LevelFailed, new System.Collections.Generic.Dictionary<string, object>
        {
            { "level_name", LocalMapData.name },
            { "matchDuration", GameTimeInSeconds }
        });

        LevelLost?.Invoke();
    }

    public void Dispose()
    {
        GridController.Instance.ActionTaken -= OnActionTaken;
        GridController.Instance.BoardUpdated -= CheckVictoryConditions;
        GridController.Instance.TileDestroyed -= OnTileDestroyed;
        GridController.Instance.GridContext.OnDestroy -= OnTileDestroyed;
    }

    private void RecordLevelDataForAnalytics()
    {

    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            ToggleWinEvent();
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            ToggleLoseEvent();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("Resetting savedata");
            Loader.Reset();
        }
    }
}