using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;

public class GridController : MonoBehaviour
{
    public static GridController Instance { get; private set; }

    [SerializeField] private int width = 6;
    [SerializeField] private int height = 8;
    [SerializeField] private TileView tilePrefab;
    [SerializeField] private Sprite sharedTileSprite;
    [SerializeField] private Transform tileContainer;
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private Vector3 gridOffset;
    [SerializeField] private bool allowInitialMatches = false;

    private ObjectPool<TileView> tilePool;
    private TileData[,] gridData;
    private TileView[,] gridViews;
    private CommandInvoker commandInvoker;
    private MatchFinder matchFinder;
    private GridContext gridContext;
    private bool isProcessingTiles;

    public Sprite Sprite => sharedTileSprite;

    public event Action TileMoved;
    public event Action TileSwapped;
    public event Action TileSwapError;
    public event Action TileDrop;
    public event Action TileDestroyed;
    public event Action<TileData[,]> BoardUpdated;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        commandInvoker = new CommandInvoker(this);
        matchFinder = new MatchFinder(width, height);

        tilePool = new ObjectPool<TileView>(
    createFunc: () => Instantiate(tilePrefab, tileContainer),
    actionOnGet: (tile) => tile.gameObject.SetActive(true),
    actionOnRelease: (tile) => tile.gameObject.SetActive(false),
    actionOnDestroy: (tile) => Destroy(tile.gameObject),
    collectionCheck: false, maxSize: 100
);

        GenerateGrid(allowInitialMatches);
        CenterCameraOnGrid();

        gridContext = new GridContext(
            gridData,
            gridViews,
            width,
            height,
            tilePool,
            commandInvoker,
            TileDestroyed
        );
    }

    public void TrySwapTiles(Vector2Int origin, Vector2Int dir)
    {
        Vector2Int target = origin + dir;

        if (!IsInsideGrid(target) || isProcessingTiles)
            return;

        var tileA = gridData[origin.x, origin.y];
        var tileB = gridData[target.x, target.y];

        if (tileA == null || tileB == null)
            return;

        var viewA = gridViews[origin.x, origin.y];
        var viewB = gridViews[target.x, target.y];

        Vector3 origPosA = viewA.transform.position;
        Vector3 origPosB = viewB.transform.position;

        isProcessingTiles = true;
        commandInvoker.AddCommand(new SwapCommand(viewA, viewB, origPosA, origPosB));
        commandInvoker.ExecuteAll();

        StartCoroutine(CheckSwapMatch(origin, target, tileA, tileB, viewA, viewB, origPosA, origPosB));
    }

    private IEnumerator CheckSwapMatch(Vector2Int origin, Vector2Int target, TileData tileA, TileData tileB, TileView viewA, TileView viewB, Vector3 origPosA, Vector3 origPosB)
    {
        isProcessingTiles = true;

        TileMoved?.Invoke();

        yield return new WaitForSeconds(0.2f);

        var tempGridData = gridData.Clone() as TileData[,];
        tempGridData[origin.x, origin.y] = tileB;
        tempGridData[target.x, target.y] = tileA;

        if (tileA.Power != TilePower.None || tileB.Power != TilePower.None)
        {
            SwapTilesInData(origin, target, tileA, tileB);

            if (tileA.Power != TilePower.None)
            {
                StartCoroutine(TriggerPowerEvent(tileA));
            }

            if (tileB.Power != TilePower.None)
            {
                StartCoroutine(TriggerPowerEvent(tileB));
            }

            yield break;
        }

        var matches = matchFinder.GetMatchGroups(tempGridData);
        if (matches.Count > 0)
        {
            SwapTilesInData(origin, target, tileA, tileB);
            StartCoroutine(MatchCycle());
        }
        else
        {
            StartCoroutine(RevertSwap(viewA, viewB, origPosA, origPosB));
        }
    }

    public void AttemptPowerTrigger(TileView tileView)
    {
        if (tileView == null || tileView.Data == null || tileView.Data.Power == TilePower.None)
        {
            Debug.LogWarning("Attempted to trigger power on a tile that has power: " + tileView.Data.Power);
            TileSwapError?.Invoke();
            return;
        }

        StartCoroutine(TriggerPowerEvent(tileView.Data));
    }

    private IEnumerator TriggerPowerEvent(TileData tileData)
    {
        gridContext.TriggerTilePower(tileData.GridPosition);
        Debug.Log($"Triggering power for tile at {tileData.GridPosition} with power {tileData.Power}");
        yield return new WaitForSeconds(0.1f);

        StartCoroutine(MatchCycle());
    }

    private void SwapTilesInData(Vector2Int origin, Vector2Int target, TileData tileA, TileData tileB)
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

    private IEnumerator RevertSwap(TileView tileA, TileView tileB, Vector3 origPosA, Vector3 origPosB)
    {
        //Debug.Log("Reverting swap...");
        // Kill any existing tweens just in case
        tileA.transform.DOKill();
        tileB.transform.DOKill();

        TileSwapError?.Invoke();

        // Animate both tiles back to their original positions
        yield return DOTween.Sequence()
            .Join(tileA.transform.DOMove(origPosA, 0.25f).SetEase(Ease.OutBack))
            .Join(tileB.transform.DOMove(origPosB, 0.25f).SetEase(Ease.OutBack))
            .WaitForCompletion();

        isProcessingTiles = false;
    }

    private bool IsInsideGrid(Vector2Int pos) => pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;

    private IEnumerator MatchCycle()
    {
        TileSwapped?.Invoke();
        yield return new WaitForSeconds(0.1f);

        while (true)
        {
            yield return new WaitUntil(() => commandInvoker.IsEmpty());
            yield return new WaitUntil(() => !AnyTileTweening());

            var matchGroups = matchFinder.GetMatchGroups(gridData);
            if (matchGroups.Count == 0)
                break;

            // List to keep track of power tile positions
            var powerTilePositions = new HashSet<Vector2Int>();

            // Create and execute the CreatePowerTileCommand
            var createPowerTileCommand = new CreatePowerTileCommand(
                matchGroups,
                gridData,
                gridViews,
                matchFinder.DetermineMatchShape,
                (origin, type, power) =>
                {
                    var newData = new TileData(type, origin, power);
                    powerTilePositions.Add(origin); // Directly add the position to the HashSet
                    return newData;
                }
            );

            // Execute the power tile command immediately
            yield return createPowerTileCommand.Execute();

            // Filter out power tile positions from the destruction list
            var flatMatches = matchGroups.SelectMany(g => g).Distinct().Where(pos => !powerTilePositions.Contains(pos)).ToList();

            commandInvoker.AddCommand(
                new DestroyCommand(flatMatches, gridViews, gridData, tilePool, TileDestroyed, gridContext)
            );
            commandInvoker.AddCommand(new DropCommand(gridData, gridViews, width, height, GridToWorldPos));
            commandInvoker.ExecuteAll();
        }

        commandInvoker.AddCommand(new RefillCommand(gridData, gridViews, width, height, CreateTileAt, GridToWorldPos, TileDrop));
        commandInvoker.ExecuteAll();

        yield return new WaitUntil(() => commandInvoker.IsEmpty());
        yield return new WaitUntil(() => !AnyTileTweening());

        var refillMatches = matchFinder.GetMatchGroups(gridData);
        if (refillMatches.Count > 0)
        {
            StartCoroutine(MatchCycle());
            yield break;
        }

        isProcessingTiles = false;

        BoardUpdated?.Invoke(gridData);
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
        var type = GetRandomTileType();
        var data = new TileData(type, new Vector2Int(x, y));

        var view = tilePool.Get();
        view.transform.localScale = Vector3.one;
        view.Init(data, sharedTileSprite);
        view.gameObject.name = $"Tile_{x}_{y}";

        gridData[x, y] = data;
        gridViews[x, y] = view;

        return view;
    }

    private void ClearBoard()
    {
        if (gridViews != null)
        {
            foreach (var view in gridViews)
            {
                if (view != null)
                    tilePool.Release(view);
            }
        }

        gridData = null;
        gridViews = null;
    }

    public void ShuffleBoard()
    {
        Debug.Log("Shuffling board with possible matches...");
        GenerateGrid(true);
    }

    public void GenerateGrid(bool allowMatches)
    {
        bool hasMatches;

        do
        {
            ClearBoard();

            gridData = new TileData[width, height];
            gridViews = new TileView[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var type = (TileType)UnityEngine.Random.Range(0, System.Enum.GetValues(typeof(TileType)).Length);
                    var position = new Vector2Int(x, y);
                    var data = new TileData(type, position);

                    var view = tilePool.Get();
                    view.transform.SetParent(transform, false);
                    view.transform.position = GridToWorldPos(position);
                    view.Init(data, sharedTileSprite);
                    view.gameObject.name = $"Tile_{x}_{y}";

                    gridData[x, y] = data;
                    gridViews[x, y] = view;
                }
            }

            hasMatches = matchFinder.GetMatchGroups(gridData).Count > 0;

        } while (!allowMatches && hasMatches);

        tileSize = tilePrefab.GetComponent<SpriteRenderer>().bounds.size.x;
    }

    private Vector3 GridToWorldPos(Vector2Int gridPosition)
    {
        return new Vector3(gridPosition.x * tileSize, gridPosition.y * tileSize, 0f) + gridOffset;
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
        return (TileType)UnityEngine.Random.Range(0, Enum.GetValues(typeof(TileType)).Length);
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

        commandInvoker.AddCommand(new DestroyCommand(flatMatches, gridViews, gridData, tilePool, TileDestroyed));
        commandInvoker.AddCommand(new DropCommand(gridData, gridViews, width, height, GridToWorldPos));
        commandInvoker.ExecuteAll();

        StartCoroutine(MatchCycle());
    }
}