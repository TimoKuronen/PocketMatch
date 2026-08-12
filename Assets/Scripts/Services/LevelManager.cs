using System;
using System.Threading;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Cysharp.Threading.Tasks;

public class LevelManager : ILevelManager, IDebugLevelTarget, IDisposable, IStartable
{
    #region Properties

    public int MovesRemaining { get; private set; }
    public bool IsLevelEnded { get; private set; }
    public MapData LocalMapData { get; private set; }
    public VictoryConditions VictoryConditions { get; private set; }
    public Action OnVictoryConditionsUpdated { get; set; }
    public Action OnLevelWon { get; set; }
    public Action OnLevelLost { get; set; }
    public Action OnLevelContinued { get; set; }
    public int GameTimeInSeconds { get; private set; }

    #endregion

    #region Fields

    private IGameSessionService gameSessionService;
    private IGridController gridController;
    private ILevelEarningsService levelEarningsService;
    private IDebugToolsService debugToolsService;
    private readonly CancellationTokenSource cts = new();

    #endregion

    #region Lifecycle

    [Inject]
    public void Construct(
        IGameSessionService gameSessionService,
        IGridController gridController,
        ILevelEarningsService levelEarningsService,
        IDebugToolsService debugToolsService)
    {
        this.gameSessionService = gameSessionService;
        this.gridController = gridController;
        this.levelEarningsService = levelEarningsService;
        this.debugToolsService = debugToolsService;
    }

    public void Start()
    {
        GameTimerAsync().Forget();
        GameSignals.OnSessionLoaded += OnSessionLoaded;

        if (GameSignals.IsSessionLoaded)
            OnSessionLoaded();
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

        debugToolsService?.UnregisterLevelTarget(this);

        if (!cts.IsCancellationRequested)
            cts.Cancel();

        cts.Dispose();
    }

    private async UniTaskVoid GameTimerAsync()
    {
        var token = cts.Token;
        GameTimeInSeconds = 0;

        while (true)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: token);
            GameTimeInSeconds++;
        }
    }

    #endregion

    #region Session Setup

    private void OnSessionLoaded()
    {
        LocalMapData = MonoBehaviour.Instantiate(gameSessionService.CurrentMapData);

        if (LocalMapData == null)
        {
            Debug.LogError("MapData not assigned.");
            return;
        }

        MovesRemaining = LocalMapData.VictoryConditions.MoveLimit;
        IsLevelEnded = false;
        VictoryConditions = LocalMapData.VictoryConditions;

        WaitForGridInitializationAsync().Forget();
    }

    private async UniTask WaitForGridInitializationAsync()
    {
        var token = cts.Token;
        await UniTask.WaitUntil(() => gridController != null && gridController.IsBoardInitialized, cancellationToken: token);

        SubscribeToEvents();

        LevelEvents.RaiseLevelStarted(new LevelStartedEventArgs(
            gameSessionService.CurrentMapData.name,
            0,
            LocalMapData.VictoryConditions.MoveLimit));
    }

    private void SubscribeToEvents()
    {
        gridController.ActionTaken += OnActionTaken;
        gridController.BoardUpdated += CheckVictoryConditions;
        gridController.TileDestroyed += OnTileDestroyed;
        gridController.GridContext.OnDestroy += OnTileDestroyed;

        debugToolsService?.RegisterLevelTarget(this);
    }

    #endregion

    #region Victory Tracking

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
        if (IsLevelEnded)
            return;

        if (!AreVictoryConditionsMet())
        {
            if (MovesRemaining <= 0)
                ToggleLoseEvent();
        }
        else
        {
            ToggleWinEvent();
        }
    }

    private bool AreVictoryConditionsMet()
    {
        if (VictoryConditions.RequiredColorMatchCount != null && VictoryConditions.RequiredColorMatchCount.Length > 0)
        {
            foreach (var match in VictoryConditions.RequiredColorMatchCount)
            {
                if (match.TileCount > 0)
                    return false;
            }
        }

        if (VictoryConditions.DestroyableTileCount > 0)
            return false;

        return true;
    }

    private void OnActionTaken()
    {
        if (IsLevelEnded)
            return;

        MovesRemaining--;
        OnVictoryConditionsUpdated?.Invoke();
    }

    #endregion

    #region Win / Lose

    public void GrantExtraMoves(int count)
    {
        if (count <= 0 || !IsLevelEnded)
            return;

        MovesRemaining += count;
        IsLevelEnded = false;
        OnVictoryConditionsUpdated?.Invoke();
        OnLevelContinued?.Invoke();
    }

    public void ForceWin()
    {
        ToggleWinEvent();
    }

    public void ForceLose()
    {
        ToggleLoseEvent();
    }

    private void ToggleWinEvent()
    {
        if (IsLevelEnded)
            return;

        IsLevelEnded = true;

        int movesSpent = LocalMapData.VictoryConditions.MoveLimit - MovesRemaining;
        int totalEarnings = levelEarningsService.GetLevelEarnings(MovesRemaining).Total;

        LevelEvents.RaiseLevelCompleted(new LevelCompletedEventArgs(
            LocalMapData.name,
            MovesRemaining,
            movesSpent,
            totalEarnings,
            GameTimeInSeconds,
            gameSessionService.IsLevelCapReached,
            GameSignals.ActiveLevelIndex));

        OnLevelWon?.Invoke();
    }

    private void ToggleLoseEvent()
    {
        if (IsLevelEnded)
            return;

        IsLevelEnded = true;

        LevelEvents.RaiseLevelFailed(new LevelFailedEventArgs(
            LocalMapData.name,
            GameTimeInSeconds));

        OnLevelLost?.Invoke();
    }

    #endregion
}
