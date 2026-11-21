using System;
using System.Threading;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Cysharp.Threading.Tasks;

public class LevelManager : ILevelManager, IDebugLevelTarget, IDisposable, IStartable
{
    public int MovesRemaining { get; private set; }
    public bool IsLevelEnded { get; private set; }
    public MapData LocalMapData { get; private set; }
    public VictoryConditions VictoryConditions { get; private set; }
    public event Action OnVictoryConditionsUpdated;
    public event Action OnLevelWon;
    public event Action OnLevelLost;
    public event Action OnLevelContinued;
    public int GameTimeInSeconds { get; private set; }

    private IGameSessionService gameSessionService;
    private IGridController gridController;
    private ILevelEarningsService levelEarningsService;
    private IDebugToolsService debugToolsService;
    private ITunables tunables;
    private readonly CancellationTokenSource cts = new();

    [Inject]
    public void Construct(
        IGameSessionService gameSessionService,
        IGridController gridController,
        ILevelEarningsService levelEarningsService,
        IDebugToolsService debugToolsService,
        ITunables tunables)
    {
        this.gameSessionService = gameSessionService;
        this.gridController = gridController;
        this.levelEarningsService = levelEarningsService;
        this.debugToolsService = debugToolsService;
        this.tunables = tunables;
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
        }

        debugToolsService?.UnregisterLevelTarget(this);

        if (LocalMapData != null)
            UnityEngine.Object.Destroy(LocalMapData);

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

    private void OnSessionLoaded()
    {
        var sourceMapData = gameSessionService.CurrentMapData;
        if (sourceMapData == null)
        {
            Debug.LogError("MapData not assigned.");
            return;
        }

        LocalMapData = MonoBehaviour.Instantiate(sourceMapData);

        if (GameSignals.ActiveLevelIndex == 0)
            LocalMapData.VictoryConditions.MoveLimit = tunables.Level1MoveLimit;

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
            gameSessionService.CurrentMapData.GetLevelName(),
            0,
            LocalMapData.VictoryConditions.MoveLimit));
    }

    private void SubscribeToEvents()
    {
        gridController.ActionTaken += OnActionTaken;
        gridController.BoardUpdated += CheckVictoryConditions;
        gridController.TileDestroyed += OnTileDestroyed;

        debugToolsService?.RegisterLevelTarget(this);
    }

    private void OnTileDestroyed(TileData data)
    {
        VictoryRules.RegisterTileDestroyed(VictoryConditions, data);
        OnVictoryConditionsUpdated?.Invoke();
    }

    private void CheckVictoryConditions(TileData[,] obj)
    {
        if (IsLevelEnded)
            return;

        if (!VictoryRules.AreMet(VictoryConditions))
        {
            if (MovesRemaining <= 0)
                HandleLoss();
        }
        else
        {
            HandleWin();
        }
    }

    private void OnActionTaken()
    {
        if (IsLevelEnded)
            return;

        MovesRemaining--;
        OnVictoryConditionsUpdated?.Invoke();
    }

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
        HandleWin();
    }

    public void ForceLose()
    {
        HandleLoss();
    }

    private void HandleWin()
    {
        if (IsLevelEnded)
            return;

        IsLevelEnded = true;

        int movesSpent = LocalMapData.VictoryConditions.MoveLimit - MovesRemaining;
        int totalEarnings = levelEarningsService.GetLevelEarnings(MovesRemaining).Total;

        LevelEvents.RaiseLevelCompleted(new LevelCompletedEventArgs(
            gameSessionService.CurrentMapData.GetLevelName(),
            MovesRemaining,
            movesSpent,
            totalEarnings,
            GameTimeInSeconds,
            gameSessionService.IsLevelCapReached,
            GameSignals.ActiveLevelIndex));

        OnLevelWon?.Invoke();
    }

    private void HandleLoss()
    {
        if (IsLevelEnded)
            return;

        IsLevelEnded = true;

        LevelEvents.RaiseLevelFailed(new LevelFailedEventArgs(
            gameSessionService.CurrentMapData.GetLevelName(),
            GameTimeInSeconds));

        OnLevelLost?.Invoke();
    }
}
