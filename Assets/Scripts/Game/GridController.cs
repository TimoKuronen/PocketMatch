using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridController : MonoBehaviour
{
    public static GridController Instance { get; private set; }

    [Header("Grid Settings")]
    [SerializeField] private int width = 6;
    [SerializeField] private int height = 8;
    [SerializeField] private TileView normalTilePrefab;
    [SerializeField] private TileView blockedTilePrefab;
    [SerializeField] private TileView breakableTilePrefab;
    [SerializeField] private RectTransform tileContainer;
    [SerializeField] private RectTransform tileFramePrefab;
    [SerializeField] private float tileSize = 1f;

    [Header("Initial Debugging Settings")]
    [SerializeField] private bool allowInitialMatches = false;

    private TileData[,] gridData;
    private TileView[,] gridViews;
    private CommandInvoker commandInvoker;
    private MapData mapData;
    private TilePoolManager tilePoolManager;
    private BoardStateEvaluator boardStateEvaluator;
    public bool IsBoardInitialized { get; private set; } = false;
    public bool IsProcessingTiles { get; private set; }
    public MatchFinder MatchFinder { get; private set; }
    public GridContext GridContext { get; private set; }
    public BoardStateEvaluator BoardEvaluator => boardStateEvaluator;

    public event Action ActionTaken;
    public event Action TileMoved;
    public event Action TileSwapped;
    public event Action TileSwapError;
    public event Action TileDrop;
    public event Action<TileData> TileDestroyed;
    public event Action<TileData[,]> BoardUpdated;
    public event Action<TileData> PowerTileCreated;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private IEnumerator Start()
    {
        yield return new WaitUntil(() => Services.Get<IGameSessionService>().IsLevelDataLoaded);

        commandInvoker = new CommandInvoker(this);
        MatchFinder = new MatchFinder(width, height);

        tilePoolManager = new TilePoolManager(
            normalTilePrefab,
            blockedTilePrefab,
            breakableTilePrefab,
            tileContainer
        );

        mapData = Services.Get<IGameSessionService>().CurrentMapData;

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

    private IEnumerator CheckSwapMatch(Vector2Int origin, Vector2Int target, TileData tileA, TileData tileB, TileView viewA, TileView viewB, Vector2 origPosA, Vector2 origPosB)
    {
        IsProcessingTiles = true;
        TileMoved?.Invoke();

        // Wait briefly for the swap animation to complete visually
        yield return new WaitForSeconds(0.2f);

        // Create a temp copy to test for matches
        var tempGridData = gridData.Clone() as TileData[,];
        tempGridData[origin.x, origin.y] = tileB;
        tempGridData[target.x, target.y] = tileA;

        // --- Handle power tiles immediately ---
        if (tileA.Power != TilePower.None || tileB.Power != TilePower.None)
        {
            SwapTilesInData(origin, target, tileA, tileB);

            if (tileA.Power != TilePower.None)
                StartCoroutine(TriggerPowerEvent(tileA));

            if (tileB.Power != TilePower.None)
                StartCoroutine(TriggerPowerEvent(tileB));

            yield break;
        }

        // --- Check for valid matches after the swap ---
        var matches = MatchFinder.GetMatchGroups(tempGridData);
        if (matches.Count > 0)
        {
            SwapTilesInData(origin, target, tileA, tileB);
            StartCoroutine(MatchCycle());
            ActionTaken?.Invoke();
        }
        else
        {
            TileSwapError?.Invoke();

            // swap them back to their original positions
            var revertCommand = new SwapCommand(viewA, viewB, origPosB, origPosA, 0.25f, Ease.OutBack);

            yield return revertCommand.Execute();

            IsProcessingTiles = false;
        }
    }

    /// <summary>
    /// Input based trigger for tile power.
    /// </summary>
    /// <param name="tileView"></param>
    public void AttemptPowerTrigger(TileView tileView)
    {
        if (tileView == null || tileView.Data == null || tileView.Data.Power == TilePower.None)
        {
            Debug.LogWarning("Attempted to trigger power on a tile that has power: " + tileView.Data.Power);
            TileSwapError?.Invoke();
            return;
        }

        ActionTaken?.Invoke();

        StartCoroutine(TriggerPowerEvent(tileView.Data));
    }

    private IEnumerator TriggerPowerEvent(TileData tileData)
    {
        GridContext.TriggerTilePower(tileData.GridPosition);
        commandInvoker.ExecuteAll();

        //Debug.Log($"Triggering power for tile at {tileData.GridPosition} with power {tileData.Power}");

        // Wait until all destroy commands from power are complete
        //  Debug.Log("# 1 Waiting for commandInvoker to empty...");
        yield return new WaitUntil(() => commandInvoker.IsEmpty());
        // Debug.Log("CommandInvoker empty!");
        //  Debug.Log("# 1 Waiting for tweens to finish...");
        yield return new WaitUntil(() => !AnyTileTweening());
        // Debug.Log("Tweens done!");

        // now that grid is cleared, run Drop and Refill
        commandInvoker.AddCommand(new DropCommand(gridData, gridViews, width, height, GridToUIPos));
        commandInvoker.AddCommand(new RefillCommand(gridData, gridViews, width, height, CreateTileAt, GridToUIPos, TileDrop));
        commandInvoker.ExecuteAll();

        //  Debug.Log("# 2 Waiting for commandInvoker to empty...");
        yield return new WaitUntil(() => commandInvoker.IsEmpty());
        // Debug.Log("CommandInvoker empty!");
        // Debug.Log("# 2 Waiting for tweens to finish...");
        yield return new WaitUntil(() => !AnyTileTweening());
        //Debug.Log("Tweens done!");

        // Finally continue match cycle
        StartCoroutine(MatchCycle());
    }

    public void SwapTilesInData(Vector2Int origin, Vector2Int target, TileData tileA, TileData tileB)
    {
        gridData[origin.x, origin.y] = tileB;
        gridData[target.x, target.y] = tileA;

        tileA.GridPosition = target;
        tileB.GridPosition = origin;

        var viewA = gridViews[origin.x, origin.y];
        var viewB = gridViews[target.x, target.y];

        gridViews[origin.x, origin.y] = viewB;
        gridViews[target.x, target.y] = viewA;
    }

    public IEnumerator MatchCycle()
    {
        Debug.Log("Starting match cycle...");
        IsProcessingTiles = true;
        int cycleCount = 0;

        bool changed;

        do
        {
            changed = false;

            // --- 1. Drop existing tiles ---
            yield return new DropCommand(gridData, gridViews, width, height, GridToUIPos).Execute();

            // Debug.Log("Board after drop:");

            // --- 2. Refill empty cells ---
            yield return new RefillCommand(gridData, gridViews, width, height, CreateTileAt, GridToUIPos, TileDrop).Execute();

            //Debug.Log("Board after refill:");

            // --- 3. Wait for animations ---
            //Debug.Log("Waiting for tweens to finish...");
            yield return new WaitUntil(() => !AnyTileTweening());
            //Debug.Log("Tweens done!");

            // --- 4. If any refillable cells are still empty, keep looping ---
            if (HasEmptyNormalSlots())
            {
                Debug.Log("Still has empty slots after refill, continuing cycle...");
                changed = true;
                cycleCount++;
                continue; // don’t check matches until board is physically stable
            }

            // --- 5. Find matches ---
            var matchGroups = MatchFinder.GetMatchGroups(gridData);
            if (matchGroups.Count > 0)
            {
                changed = true;

                // Debug.Log($"Found more match groups.");
                // Adjacent destroyables
                var destroyedNeighbours = AdjacentDamageProcessor.GetAdjacentDestroyables(matchGroups, gridData);

                // Track power tile creation
                var powerTilePositions = new HashSet<Vector2Int>();

                var createPowerTileCommand = new CreatePowerTileCommand(
                    matchGroups, gridData, gridViews, MatchFinder.DetermineMatchShape,
                    (origin, type, power) =>
                    {
                        var newData = new TileData(type, origin, power);
                        powerTilePositions.Add(origin);
                        PowerTileCreated?.Invoke(newData);
                        return newData;
                    }
                );

                // Execute power tile creation immediately
                yield return createPowerTileCommand.Execute();

                // Filter matches (exclude power tile positions)
                var flatMatches = matchGroups
                  .SelectMany(g => g) // Flatten all match groups into a single sequence of tile positions
                  .Distinct() // Remove any duplicate tile positions
                  .Where(pos => !powerTilePositions.Contains(pos)) // Exclude positions of power tiles we just created
                  .Concat(destroyedNeighbours) // Add tiles that should be destroyed due to adjacency or other effects
                  .Distinct() // Remove duplicates again (some neighbours may already be in matches)
                  .ToList(); // Materialize into a List<Vector2Int>

                // Destroy tiles
                TileSwapped?.Invoke();
                yield return new DestroyCommand(flatMatches, gridViews, gridData, tilePoolManager, TileDestroyed, GridContext).Execute();
            }

            cycleCount++;
            //Debug.Log($"Match cycle iteration {cycleCount} complete.");

        } while (changed);

        IsProcessingTiles = false;
        BoardUpdated?.Invoke(gridData);

        if (cycleCount > 2)
        {
            Debug.Log($"Extra automated matches occurred: {cycleCount - 2} extra cycles.");
            Services.Get<IAnalyticsManager>().LogEvent(AnalyticsEvents.ExtraAutomatedMatches, new Dictionary<string, object>
        {
            { "level_name", Services.Get<ILevelManager>().LocalMapData.GetLevelName() },
            { "moves_spent", cycleCount-2 }
        });
        }

        var moves = boardStateEvaluator.CountPotentialMoves();
        if (moves.TotalMoves == 0)
        {
            Debug.Log($"No moves left! (Swaps: {moves.SwapMoveCount}, Power: {moves.PowerTileMoveCount})");
            boardStateEvaluator.ShuffleBoard();
        }
    }

    /// <summary>
    /// Checks if there are any empty cells that could be refilled (normal slots only).
    /// </summary>
    private bool HasEmptyNormalSlots()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var data = gridData[x, y];
                if (data == null)
                    return true; // refillable hole
                if (data.State == TileState.Empty)
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

    private TileView CreateTileAt(int x, int y)
    {
        var data = gridData[x, y];
        if (data == null)
        {
            Debug.Log($"Attempted to create tile at ({x}, {y}) but gridData is null.");

            for (int i = 0; i < gridData.GetLength(0); i++)
            {
                for (int z = 0; z < gridData.GetLength(1); z++)
                {
                    if (data == null)
                    {
                        Debug.Log($"Tile is null or blocked");
                    }
                    else Debug.Log($"Tile is normal");
                }
            }
            return null;
        }
        data.State = TileState.Normal;
        data.Type = GetRandomTileType();

        var view = tilePoolManager.GetForState(TileState.Normal);
        view.ViewKind = TileState.Normal;
        view.transform.localScale = Vector3.one;
        view.Init(data);
        view.gameObject.name = $"Tile_{x}_{y}\"";

        gridViews[x, y] = view;

        return view;
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

    private void GenerateGrid(bool allowMatches)
    {
        ClearBoard();

        gridData = LevelBuilder.BuildLevelFromMapData(mapData);
        gridViews = new TileView[gridData.GetLength(0), gridData.GetLength(1)];

        // Randomize normal tiles
        for (int x = 0; x < gridData.GetLength(0); x++)
        {
            for (int y = 0; y < gridData.GetLength(1); y++)
            {
                var data = gridData[x, y];
                if (data == null || data.State != TileState.Normal)
                    continue;
                data.Type = GetRandomTileType();
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
                        data.Type = GetRandomTileType();
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

        // Use RectTransform size for tile size
        tileSize = normalTilePrefab.GetComponent<RectTransform>().sizeDelta.x;

        LevelBuilder.SpawnGridViews(gridData, gridViews, tilePoolManager, tileContainer);
        LevelBuilder.SpawnGridFrames(width, height, tileFramePrefab, tileContainer);
    }

    public Vector2 GridToUIPos(Vector2Int gridPos)
    {
        float boardWidth = width * tileSize;
        float boardHeight = height * tileSize;

        // Pivot-corrected offset
        float offsetX = -tileContainer.pivot.x * tileContainer.rect.width + (tileContainer.rect.width - boardWidth) / 2f;
        float offsetY = -tileContainer.pivot.y * tileContainer.rect.height + (tileContainer.rect.height - boardHeight) / 2f;

        float x = gridPos.x * tileSize + offsetX + tileSize / 2f;
        float y = gridPos.y * tileSize + offsetY + tileSize / 2f;

        return new Vector2(x, y);
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

    private TileType GetRandomTileType()
    {
        return mapData.AllowedTileColors[UnityEngine.Random.Range(0, mapData.AllowedTileColors.Length)];
    }

    /// <summary>
    /// Used for debugging to destroy individual tiles without matches
    /// </summary>
    /// <param name="origin"></param>
    public void DestroyTargetTile(Vector2Int origin)
    {
        List<Vector2Int> flatMatches = new();
        var tileA = gridData[origin.x, origin.y];
        flatMatches.Add(origin);

        TileSwapped?.Invoke();

        commandInvoker.AddCommand(new DestroyCommand(flatMatches, gridViews, gridData, tilePoolManager, TileDestroyed));
        commandInvoker.AddCommand(new DropCommand(gridData, gridViews, width, height, GridToUIPos));
        commandInvoker.ExecuteAll();

        StartCoroutine(MatchCycle());
    }
}