using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VContainer;

public class GridController : MonoBehaviour, IGridController
{
    #region Fields & Properties

    [Header("Settings")]
    [SerializeField] private GridControllerSettings settings;
    [SerializeField] private RectTransform tileContainer;

    private TileData[,] gridData;
    private TileView[,] gridViews;
    private CommandInvoker commandInvoker;
    private MapData mapData;
    private TilePoolManager tilePoolManager;
    private BoardStateEvaluator boardStateEvaluator;
    private IGameSessionService gameSessionService;
    private IAnalyticsService analyticsService;

    private int width;
    private int height;
    private float tileSize;
    private bool allowInitialMatches;
    private Vector2Int? lastMovedTilePosition;

    public bool IsBoardInitialized { get; private set; } = false;
    public bool IsProcessingTiles { get; private set; }
    public MatchFinder MatchFinder { get; private set; }
    public GridContext GridContext { get; private set; }
    public BoardStateEvaluator BoardEvaluator => boardStateEvaluator;

    #endregion

    #region Events

    public event Action ActionTaken;
    public event Action TileMoved;
    public event Action TileSwapped;
    public event Action TileSwapError;
    public event Action TileDrop;
    public event Action<TileData> TileDestroyed;
    public event Action<TileData[,]> BoardUpdated;
    public event Action<TileData> PowerTileCreated;
    public event Action OnBoardShuffle;

    #endregion

    #region Unity Lifecycle

    [Inject]
    public void Construct(IGameSessionService gameSessionService, IAnalyticsService analyticsService)
    {
        this.gameSessionService = gameSessionService;
        this.analyticsService = analyticsService;
    }

    private IEnumerator Start()
    {
        if (settings == null)
        {
            Debug.LogError("GridControllerSettings is not assigned!");
            yield break;
        }

        width = settings.width;
        height = settings.height;
        tileSize = settings.tileSize;
        allowInitialMatches = settings.allowInitialMatches;

        yield return new WaitUntil(() => GameSignals.IsSessionLoaded);

        Debug.Log("GridController starting with map data: " + gameSessionService.CurrentMapData);

        commandInvoker = new CommandInvoker(this);
        MatchFinder = new MatchFinder(width, height);

        tilePoolManager = new TilePoolManager(
            settings.normalTilePrefab,
            settings.blockedTilePrefab,
            settings.breakableTilePrefab,
            tileContainer,
            this
        );

        mapData = gameSessionService.CurrentMapData;

        GenerateGrid(allowInitialMatches);
        CenterCameraOnGrid();

        GridContext = new GridContext(
            gridData,
            gridViews,
            width,
            height,
            tilePoolManager,
            commandInvoker,
            TileDestroyed
        );

        boardStateEvaluator = new BoardStateEvaluator(gridData, gridViews, width, height, this);

        yield return new WaitForSeconds(0.5f);

        BoardUpdated?.Invoke(gridData);
        IsBoardInitialized = true;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Attempts to swap two adjacent tiles and checks for valid matches.
    /// </summary>
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

        var rectA = viewA.GetComponent<RectTransform>();
        var rectB = viewB.GetComponent<RectTransform>();
        Vector2 origPosA = rectA.anchoredPosition;
        Vector2 origPosB = rectB.anchoredPosition;

        IsProcessingTiles = true;
        commandInvoker.AddCommand(new SwapCommand(viewA, viewB, origPosA, origPosB));
        commandInvoker.ExecuteAll();

        StartCoroutine(CheckSwapMatch(origin, target, tileA, tileB, viewA, viewB, origPosA, origPosB));
    }

    /// <summary>
    /// Manually triggers a power tile's effect.
    /// </summary>
    public void AttemptPowerTrigger(TileView tileView)
    {
        if (tileView == null || tileView.Data == null || tileView.Data.Power == TilePower.None)
        {
            Debug.LogWarning("Attempted to trigger power on a tile that has power: " + tileView.Data.Power);
            TileSwapError?.Invoke();
            return;
        }

        ActionTaken?.Invoke();
        StartCoroutine(TriggerPowerEvent(tileView.Data, TileType.None));
    }

    /// <summary>
    /// Swaps tile data and views between two grid positions.
    /// </summary>
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

    /// <summary>
    /// Main match resolution cycle that handles gravity, refilling, matching, and cascading effects.
    /// </summary>
    public IEnumerator MatchCycle()
    {
        IsProcessingTiles = true;
        int cycleCount = 0;
        bool changed;
        Vector2Int? movedTileForThisCycle = lastMovedTilePosition;

        do
        {
            changed = false;

            yield return new GravityCommand(gridData, gridViews, width, height, GridToUIPos, CreateTileAt, mapData).Execute();
            yield return new WaitUntil(() => !AnyTileTweening());

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

                yield return createPowerTileCommand.Execute();

                var flatMatches = matchGroups
                    .SelectMany(g => g)
                    .Distinct()
                    .Where(pos => !powerTilePositions.Contains(pos))
                    .Concat(destroyedNeighbours)
                    .Distinct()
                    .ToList();

                TileSwapped?.Invoke();
                yield return new DestroyCommand(flatMatches, gridViews, gridData, tilePoolManager, TileDestroyed, GridContext).Execute();
            }

            cycleCount++;

        } while (changed);

        // Reset the last moved tile position after the match cycle completes
        lastMovedTilePosition = null;

        IsProcessingTiles = false;
        BoardUpdated?.Invoke(gridData);

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
            boardStateEvaluator.ShuffleBoard();
        }
    }

    /// <summary>
    /// Converts grid coordinates to UI anchored position.
    /// </summary>
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

    /// <summary>
    /// Debug method to destroy a single tile and trigger match cycle.
    /// </summary>
    public void DestroyTargetTile(Vector2Int origin)
    {
        List<Vector2Int> flatMatches = new();
        flatMatches.Add(origin);

        TileSwapped?.Invoke();

        commandInvoker.AddCommand(new DestroyCommand(flatMatches, gridViews, gridData, tilePoolManager, TileDestroyed, GridContext));
        commandInvoker.ExecuteAll();

        StartCoroutine(MatchCycle());
    }

    #endregion

    #region Private Methods - Tile Operations

    private IEnumerator CheckSwapMatch(Vector2Int origin, Vector2Int target, TileData tileA, TileData tileB, TileView viewA, TileView viewB, Vector2 origPosA, Vector2 origPosB)
    {
        IsProcessingTiles = true;
        TileMoved?.Invoke();

        yield return new WaitForSeconds(0.2f);

        var tempGridData = gridData.Clone() as TileData[,];
        tempGridData[origin.x, origin.y] = tileB;
        tempGridData[target.x, target.y] = tileA;

        if (tileA.Power != TilePower.None || tileB.Power != TilePower.None)
        {
            SwapTilesInData(origin, target, tileA, tileB);

            if (tileA.Power != TilePower.None)
                StartCoroutine(TriggerPowerEvent(tileA, tileB.Type));

            if (tileB.Power != TilePower.None)
                StartCoroutine(TriggerPowerEvent(tileB, tileA.Type));

            yield break;
        }

        var matches = MatchFinder.GetMatchGroups(tempGridData);
        if (matches.Count > 0)
        {
            SwapTilesInData(origin, target, tileA, tileB);
            // Track the position where the tile ended up (target is where tileA moved to)
            lastMovedTilePosition = target;
            StartCoroutine(MatchCycle());
            ActionTaken?.Invoke();
        }
        else
        {
            TileSwapError?.Invoke();
            var revertCommand = new SwapCommand(viewA, viewB, origPosB, origPosA, Ease.OutBack);
            yield return revertCommand.Execute();
            IsProcessingTiles = false;
        }
    }

    private IEnumerator TriggerPowerEvent(TileData tileData, TileType matchedWithTile)
    {
        GridContext.TriggerTilePower(tileData.GridPosition, matchedWithTile);
        commandInvoker.ExecuteAll();

        yield return new WaitUntil(() => commandInvoker.IsEmpty());
        yield return new WaitUntil(() => !AnyTileTweening());

        commandInvoker.AddCommand(new GravityCommand(gridData, gridViews, width, height, GridToUIPos, CreateTileAt, mapData));
        commandInvoker.ExecuteAll();

        yield return new WaitUntil(() => commandInvoker.IsEmpty());
        yield return new WaitUntil(() => !AnyTileTweening());

        StartCoroutine(MatchCycle());
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

    #region Private Methods - Grid State

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

    #region Private Methods - Grid Generation

    private void GenerateGrid(bool allowMatches)
    {
        ClearBoard();

        gridData = LevelBuilder.BuildLevelFromMapData(mapData);
        gridViews = new TileView[gridData.GetLength(0), gridData.GetLength(1)];

        for (int x = 0; x < gridData.GetLength(0); x++)
        {
            for (int y = 0; y < gridData.GetLength(1); y++)
            {
                var data = gridData[x, y];
                if (data == null || data.State != TileState.Normal)
                    continue;
                data.Type = GridHelperMethods.GetRandomTileType(mapData);
            }
        }

        if (!allowMatches)
        {
            bool hasMatches;
            int safeguard = 100;

            do
            {
                for (int x = 0; x < gridData.GetLength(0); x++)
                {
                    for (int y = 0; y < gridData.GetLength(1); y++)
                    {
                        var data = gridData[x, y];
                        if (data == null || data.State != TileState.Normal) continue;
                        data.Type = GridHelperMethods.GetRandomTileType(mapData);
                    }
                }

                hasMatches = MatchFinder.GetMatchGroups(gridData).Count > 0;
                safeguard--;
                if (safeguard <= 0)
                {
                    Debug.LogWarning("Safeguard hit: could not generate grid without matches.");
                    break;
                }
            } while (hasMatches);
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
            foreach (var view in gridViews)
            {
                if (view != null && view.Data != null)
                    tilePoolManager.Release(view);
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