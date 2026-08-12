using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using Cysharp.Threading.Tasks;

public class GridController : MonoBehaviour, IGridController
{
    #region Fields

    [Header("Settings")]
    [SerializeField] private GridControllerSettings settings;
    [SerializeField] private PowerVfxSettings powerVfxSettings;
    [SerializeField] private RectTransform tileContainer;

    private TileData[,] gridData;
    private TileView[,] gridViews;
    private CommandInvoker commandInvoker;
    private MapData mapData;
    private TilePoolManager tilePoolManager;
    private BoardStateEvaluator boardStateEvaluator;
    private IGameSessionService gameSessionService;
    private IAnalyticsService analyticsService;
    private IEffectService effectService;

    private int width;
    private int height;
    private float tileSize;
    private Vector2Int? lastMovedTilePosition;

    public bool IsBoardInitialized { get; private set; } = false;
    public bool IsProcessingTiles { get; private set; }
    public MatchFinder MatchFinder { get; private set; }
    public GridContext GridContext { get; private set; }
    public BoardStateEvaluator BoardEvaluator => boardStateEvaluator;

#if UNITY_EDITOR
    /// <summary>For editor validation only: data and view arrays + dimensions.</summary>
    public TileData[,] GridDataForValidation => gridData;
    public TileView[,] GridViewsForValidation => gridViews;
    public int GridWidthForValidation => width;
    public int GridHeightForValidation => height;
#endif

    #endregion

    #region Events
    public event Action TileDrop;
    public event Action ActionTaken;
    public event Action TileMoved;
    public event Action TileSwapped;
    public event Action TileSwapError;
    public event Action<TileData> TileDestroyed;
    public event Action TilesDestroyed;
    public event Action<TileData[,]> BoardUpdated;
    public event Action<TileData> PowerTileCreated;
    public event Action OnBoardShuffle;

    #endregion

    #region Lifecycle

    [Inject]
    public void Construct(IGameSessionService gameSessionService, IAnalyticsService analyticsService, IEffectService effectService)
    {
        this.gameSessionService = gameSessionService;
        this.analyticsService = analyticsService;
        this.effectService = effectService;
    }

    private void Start()
    {
        StartAsync().Forget();
    }

    private async UniTask StartAsync()
    {
        var token = this.GetCancellationTokenOnDestroy();
        if (settings == null)
        {
            Debug.LogError("GridControllerSettings is not assigned!");
            return;
        }

        width = settings.width;
        height = settings.height;
        tileSize = settings.tileSize;

        await UniTask.WaitUntil(() => GameSignals.IsSessionLoaded, cancellationToken: token);

        commandInvoker = new CommandInvoker();
        MatchFinder = new MatchFinder(width, height);

        tilePoolManager = new TilePoolManager(
            settings.normalTilePrefab,
            settings.blockedTilePrefab,
            settings.breakableTilePrefab,
            tileContainer,
            this
        );

        mapData = gameSessionService.CurrentMapData;

        GenerateGrid();
        CenterCameraOnGrid();

        GridContext = new GridContext(
            gridData,
            gridViews,
            width,
            height,
            tilePoolManager,
            commandInvoker,
            TileDestroyed,
            () => TilesDestroyed?.Invoke()
        );
        GridContext.GridController = this;
        GridContext.EffectService = effectService;
        GridContext.PowerVfxSettings = powerVfxSettings;

        boardStateEvaluator = new BoardStateEvaluator(gridData, gridViews, width, height, this);

        await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: token);

        BoardUpdated?.Invoke(gridData);
        BoardDebugHooks.NotifyBoardInitialized(this, gridData);
        IsBoardInitialized = true;
    }

    #endregion

    #region Public API

    public void TrySwapTiles(Vector2Int origin, Vector2Int dir)
    {
        Vector2Int target = origin + dir;

        if (!GridHelperMethods.IsInsideGrid(target, width, height) || IsProcessingTiles)
            return;

        var tileA = gridData[origin.x, origin.y];
        var tileB = gridData[target.x, target.y];

        if (tileA == null || tileB == null || tileB.State != TileState.Normal)
            return;

        var viewA = gridViews[origin.x, origin.y];
        var viewB = gridViews[target.x, target.y];

        Vector2 origPosA = viewA.RectTransform.anchoredPosition;
        Vector2 origPosB = viewB.RectTransform.anchoredPosition;

        IsProcessingTiles = true;
        commandInvoker.AddCommand(new SwapCommand(viewA, viewB, origPosA, origPosB));
        commandInvoker.ExecuteAll();

        CheckSwapMatchAsync(origin, target, tileA, tileB, viewA, viewB, origPosA, origPosB).Forget();
    }

    public void AttemptPowerTrigger(TileView tileView)
    {
        if (tileView == null || tileView.Data == null || tileView.Data.Power == TilePower.None)
        {
            Debug.LogWarning("Attempted to trigger power on a tile that has power: " + tileView.Data.Power);
            TileSwapError?.Invoke();
            return;
        }

        ActionTaken?.Invoke();
        TriggerPowerEventAsync(tileView.Data, TileType.None).Forget();
    }

    public void SwapTilesInData(Vector2Int origin, Vector2Int target, TileData tileA, TileData tileB)
    {
        gridData[origin.x, origin.y] = tileB;
        gridData[target.x, target.y] = tileA;

        var viewA = gridViews[origin.x, origin.y];
        var viewB = gridViews[target.x, target.y];

        gridViews[origin.x, origin.y] = viewB;
        gridViews[target.x, target.y] = viewA;

        GridHelperMethods.UpdateTilePosition(tileA, viewA, target);
        GridHelperMethods.UpdateTilePosition(tileB, viewB, origin);
    }

    public async UniTask MatchCycleAsync()
    {
        var token = this.GetCancellationTokenOnDestroy();
        IsProcessingTiles = true;
        int cycleCount = 0;
        bool changed;
        Vector2Int? movedTileForThisCycle = lastMovedTilePosition;

        do
        {
            changed = false;

            await new GravityCommand(gridData, gridViews, width, height, GridToUIPos, CreateTileAt, mapData, () => TileDrop?.Invoke())
                .ExecuteAsync();
            await UniTask.WaitUntil(() => !AnyTileTweening(), cancellationToken: token);

            if (HasEmptyNormalSlots())
            {
                changed = true;
                cycleCount++;
                continue;
            }

            var matchGroups = MatchFinder.GetMatchGroups(gridData);
            if (matchGroups.Count > 0)
            {
                changed = true;

                var destroyedNeighbours = AdjacentDamageProcessor.GetAdjacentDestroyables(matchGroups, gridData);
                var powerTilePositions = new HashSet<Vector2Int>();

                // Use moved tile only for the first cycle (player-initiated), not for cascading matches
                Vector2Int? tileToUse = cycleCount == 0 ? movedTileForThisCycle : null;

                var createPowerTileCommand = new CreatePowerTileCommand(
                    matchGroups, gridData, gridViews, MatchFinder.DetermineMatchShape,
                    (origin, type, power) =>
                    {
                        var newData = new TileData(type, origin, power);
                        powerTilePositions.Add(origin);
                        PowerTileCreated?.Invoke(newData);
                        return newData;
                    },
                    tileToUse
                );

                await createPowerTileCommand.ExecuteAsync();

                var flatMatches = new List<Vector2Int>(matchGroups.Count * 3 + destroyedNeighbours.Count);
                var seen = new HashSet<Vector2Int>();

                foreach (var group in matchGroups)
                {
                    foreach (var pos in group)
                    {
                        if (!powerTilePositions.Contains(pos) && seen.Add(pos))
                        {
                            flatMatches.Add(pos);
                        }
                    }
                }

                foreach (var pos in destroyedNeighbours)
                {
                    if (seen.Add(pos))
                    {
                        flatMatches.Add(pos);
                    }
                }

                TileSwapped?.Invoke();
                await new DestroyCommand(flatMatches, gridViews, gridData, tilePoolManager, TileDestroyed, GridContext, onDestroyBatch: () => TilesDestroyed?.Invoke())
                    .ExecuteAsync();
            }

            cycleCount++;

        } while (changed);

        // Reset the last moved tile position after the match cycle completes
        lastMovedTilePosition = null;

        IsProcessingTiles = false;
        BoardUpdated?.Invoke(gridData);
        BoardDebugHooks.NotifyBoardUpdated(this, gridData);
        if (cycleCount > 2)
        {
            analyticsService.LogEvent(AnalyticsEvents.ExtraAutomatedMatches, new Dictionary<string, object>
            {
                { "level_name", mapData.GetLevelName() ?? "Unknown" },
                { "moves_spent", cycleCount-2 }
            });
        }

        var moves = boardStateEvaluator.CountPotentialMoves();
        if (moves.TotalMoves == 0)
        {
            Debug.Log($"No moves left! (Swaps: {moves.SwapMoveCount}, Power: {moves.PowerTileMoveCount})");
            OnBoardShuffle?.Invoke();
            BoardDebugHooks.NotifyBoardShuffled(this, gridData);
            boardStateEvaluator.ShuffleBoard();
        }
    }

    public Vector2 GridToUIPos(Vector2Int gridPos)
    {
        float boardWidth = width * tileSize;
        float boardHeight = height * tileSize;

        float offsetX = -tileContainer.pivot.x * tileContainer.rect.width + (tileContainer.rect.width - boardWidth) / 2f;
        float offsetY = -tileContainer.pivot.y * tileContainer.rect.height + (tileContainer.rect.height - boardHeight) / 2f;

        float x = gridPos.x * tileSize + offsetX + tileSize / 2f;
        float y = gridPos.y * tileSize + offsetY + tileSize / 2f;

        return new Vector2(x, y);
    }

    public void DestroyTargetTile(Vector2Int origin)
    {
        List<Vector2Int> flatMatches = new();
        flatMatches.Add(origin);

        TileSwapped?.Invoke();

        commandInvoker.AddCommand(new DestroyCommand(flatMatches, gridViews, gridData, tilePoolManager, TileDestroyed, GridContext, onDestroyBatch: () => TilesDestroyed?.Invoke()));
        commandInvoker.ExecuteAll();

        MatchCycleAsync().Forget();
    }

    #endregion

    #region Private Helpers - Tile Operations

    private async UniTask CheckSwapMatchAsync(Vector2Int origin, Vector2Int target, TileData tileA, TileData tileB, TileView viewA, TileView viewB, Vector2 origPosA, Vector2 origPosB)
    {
        var token = this.GetCancellationTokenOnDestroy();
        IsProcessingTiles = true;
        TileMoved?.Invoke();

        await UniTask.Delay(TimeSpan.FromSeconds(0.2f), cancellationToken: token);

        var tempGridData = gridData.Clone() as TileData[,];
        tempGridData[origin.x, origin.y] = tileB;
        tempGridData[target.x, target.y] = tileA;

        if (tileA.Power != TilePower.None || tileB.Power != TilePower.None)
        {
            SwapTilesInData(origin, target, tileA, tileB);

            if (tileA.Power != TilePower.None)
                TriggerPowerEventAsync(tileA, tileB.Type).Forget();

            if (tileB.Power != TilePower.None)
                TriggerPowerEventAsync(tileB, tileA.Type).Forget();

            return;
        }

        var matches = MatchFinder.GetMatchGroups(tempGridData);
        if (matches.Count > 0)
        {
            SwapTilesInData(origin, target, tileA, tileB);
            // Track the position where the tile ended up (target is where tileA moved to)
            lastMovedTilePosition = target;
            MatchCycleAsync().Forget();
            ActionTaken?.Invoke();
        }
        else
        {
            TileSwapError?.Invoke();
            var revertCommand = new SwapCommand(viewA, viewB, origPosB, origPosA, Ease.OutBack);
            await revertCommand.ExecuteAsync();
            IsProcessingTiles = false;
        }
    }

    private async UniTask TriggerPowerEventAsync(TileData tileData, TileType matchedWithTile)
    {
        var token = this.GetCancellationTokenOnDestroy();
        GridContext.TriggerTilePower(tileData.GridPosition, matchedWithTile);
        commandInvoker.ExecuteAll();

        await UniTask.WaitUntil(() => commandInvoker.IsEmpty(), cancellationToken: token);
        await UniTask.WaitUntil(() => !AnyTileTweening(), cancellationToken: token);

        commandInvoker.AddCommand(new GravityCommand(gridData, gridViews, width, height, GridToUIPos, CreateTileAt, mapData, () => TileDrop?.Invoke()));
        commandInvoker.ExecuteAll();

        await UniTask.WaitUntil(() => commandInvoker.IsEmpty(), cancellationToken: token);
        await UniTask.WaitUntil(() => !AnyTileTweening(), cancellationToken: token);

        await MatchCycleAsync();
    }

    private TileView CreateTileAt(int x, int y)
    {
        var data = gridData[x, y];
        if (data == null)
        {
            Debug.LogError($"Attempted to create tile at ({x}, {y}) but gridData is null.");
            return null;
        }

        data.State = TileState.Normal;
        data.Type = GridHelperMethods.GetRandomTileType(mapData);

        var view = tilePoolManager.GetForState(TileState.Normal);
        view.ViewKind = TileState.Normal;
        view.transform.localScale = Vector3.one;
        view.Init(data);
        view.gameObject.name = $"Tile_{x}_{y}";

        gridViews[x, y] = view;
        return view;
    }

    #endregion

    #region Private Helpers - Grid State

    private bool HasEmptyNormalSlots()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var data = gridData[x, y];
                if (data == null || data.State == TileState.Empty)
                    return true;
            }
        }
        return false;
    }

    private bool AnyTileTweening()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var view = gridViews[x, y];
                if (view != null && DOTween.IsTweening(view.transform))
                    return true;
            }
        }
        return false;
    }

    #endregion

    #region Private Helpers - Grid Generation

    private void GenerateGrid()
    {
        ClearBoard();

        gridData = LevelBuilder.BuildLevelFromMapData(mapData);
        gridViews = new TileView[gridData.GetLength(0), gridData.GetLength(1)];
        int w = gridData.GetLength(0);
        int h = gridData.GetLength(1);

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                var data = gridData[x, y];
                if (data == null || data.State != TileState.Normal)
                    continue;
                data.Type = GridHelperMethods.GetRandomTileType(mapData);
            }
        }

        bool hasMatches = true;
        bool hasPotentialMoves = false;
        int safeguard = 200;

        while ((hasMatches || !hasPotentialMoves) && safeguard > 0)
        {
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    var data = gridData[x, y];
                    if (data == null || data.State != TileState.Normal) continue;
                    data.Type = GridHelperMethods.GetRandomTileType(mapData);
                }
            }

            hasMatches = MatchFinder.GetMatchGroups(gridData).Count > 0;
            hasPotentialMoves = GridHelperMethods.HasPotentialMoves(gridData, w, h);
            safeguard--;
        }

        if (safeguard <= 0)
        {
            Debug.LogWarning("GenerateGrid safeguard hit: could not get board with no matches and at least one possible move.");
        }

        if (tileSize <= 0)
        {
            tileSize = settings.normalTilePrefab.GetComponent<RectTransform>().sizeDelta.x;
        }

        LevelBuilder.SpawnGridViews(gridData, gridViews, tilePoolManager, tileContainer, this);
        LevelBuilder.SpawnGridFrames(width, height, settings.tileFramePrefab, tileContainer, this);
    }

    private void ClearBoard()
    {
        if (gridViews != null)
        {
            for (int x = 0; x < gridViews.GetLength(0); x++)
            {
                for (int y = 0; y < gridViews.GetLength(1); y++)
                {
                    var view = gridViews[x, y];
                    if (view != null && view.Data != null)
                        tilePoolManager.Release(view);
                }
            }
        }

        gridData = null;
        gridViews = null;
    }

    private void CenterCameraOnGrid()
    {
        float gridWidth = width * tileSize;
        float gridHeight = height * tileSize;
        Vector3 centerPos = new Vector3(gridWidth / 2f - tileSize / 2f, gridHeight / 2f - tileSize / 2f, -10f);

        Camera.main.transform.position = centerPos;
        Camera.main.orthographic = true;
        Camera.main.orthographicSize = Mathf.Max(gridWidth, gridHeight) - 1;
    }

    #endregion
}