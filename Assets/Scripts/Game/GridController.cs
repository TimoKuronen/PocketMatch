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

    public AudioSource AudioSource { get; private set; }

    private ObjectPool<TileView> tilePool;
    private TileData[,] gridData;
    private TileView[,] gridViews;
    private bool isProcessingTiles;

    public event Action TileMoved;
    public event Action TileSwapped;
    public event Action TileSwapError;
    public event Action TileDrop;
    public event Action TileDestroyed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        AudioSource = GetComponent<AudioSource>();
        tilePool = new ObjectPool<TileView>(
        createFunc: () => Instantiate(tilePrefab, tileContainer),
        actionOnGet: (tile) => tile.gameObject.SetActive(true),
        actionOnRelease: (tile) => tile.gameObject.SetActive(false),
        actionOnDestroy: (tile) => Destroy(tile.gameObject),
        collectionCheck: false, maxSize: 100
        );
    }

    private void Start()
    {
        GenerateGrid(allowInitialMatches);
        CenterCameraOnGrid();
    }

    public void StartMatchCycle()
    {
        StartCoroutine(MatchCycle());
    }

    public void TrySwapTiles(Vector2Int origin, Vector2Int dir)
    {
        Vector2Int target = origin + dir;
        Debug.DrawLine(GridToWorldPos(origin), GridToWorldPos(target), Color.cyan, 5f);

        if (!IsInsideGrid(target) || isProcessingTiles)
            return;

        var tileA = gridData[origin.x, origin.y];
        var tileB = gridData[target.x, target.y];

        if (tileA == null || tileB == null)
            return;

        // Save views before swap
        var viewA = gridViews[origin.x, origin.y];
        var viewB = gridViews[target.x, target.y];

        // Save their current positions BEFORE swap
        Vector3 origPosA = viewA.transform.position;
        Vector3 origPosB = viewB.transform.position;

        // Animate views
        viewA.transform.DOMove(origPosB, 0.15f);
        viewB.transform.DOMove(origPosA, 0.15f);

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
        var matches = GetAllMatches(tempGridData);
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
        Debug.Log("Reverting swap...");
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

        yield return new WaitForSeconds(0.1f); // optional initial delay

        while (true)
        {
            var allMatches = GetAllMatches(gridData);
            if (allMatches.Count == 0)
            {
                Debug.Log("No matches found, breaking cycle.");
                break;
            }

            Debug.Log($"Found {allMatches.Count} match groups.");
            var flatMatches = allMatches.SelectMany(group => group).Distinct().ToList();
            yield return DestroyMatchedTiles(allMatches);
            yield return DropTiles();
            yield return RefillBoard();
        }

        isProcessingTiles = false;
        Debug.Log("Board is stable.");
    }

    private IEnumerator RefillBoard()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (gridData[x, y] == null)
                {
                    var view = CreateTileAt(x, y, TileSpecialType.None);

                    Vector3 spawnPos = GridToWorldPos(new Vector2Int(x, y + 3));
                    Vector3 targetPos = GridToWorldPos(new Vector2Int(x, y));

                    view.transform.position = spawnPos;
                    view.transform.DOMove(targetPos, 0.25f).SetEase(Ease.OutCubic);

                    yield return new WaitForSeconds(0.02f);
                    TileDrop?.Invoke();
                }
            }
        }

        yield return new WaitForSeconds(0.3f);
    }

    private TileView CreateTileAt(int x, int y, TileSpecialType specialTile)
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

    private IEnumerator DropTiles()
    {
        for (int x = 0; x < width; x++)
        {
            int emptyY = -1;

            for (int y = 0; y < height; y++)
            {
                if (gridData[x, y] == null)
                {
                    if (emptyY == -1) emptyY = y;
                }
                else if (emptyY != -1)
                {
                    // Move data
                    gridData[x, emptyY] = gridData[x, y];
                    gridData[x, y] = null;

                    // Move view
                    gridViews[x, emptyY] = gridViews[x, y];
                    gridViews[x, y] = null;

                    // Update tile data position
                    gridData[x, emptyY].GridPosition = new Vector2Int(x, emptyY);

                    // Move visual
                    var view = gridViews[x, emptyY];
                    Vector3 targetPos = GridToWorldPos(new Vector2Int(x, emptyY));
                    view.transform.DOMove(targetPos, 0.2f).SetEase(Ease.OutCubic);

                    emptyY++;
                }
            }
        }

        yield return new WaitForSeconds(0.25f);
    }

    private IEnumerator DestroyMatchedTiles(List<List<TileData>> matchGroups)
    {
        var allMatches = matchGroups.SelectMany(g => g).Distinct().ToList();

        foreach (var tileData in allMatches)
        {
            var view = gridViews[tileData.GridPosition.x, tileData.GridPosition.y];
            if (view != null)
                view.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack);
        }

        TileDestroyed?.Invoke();

        yield return new WaitForSeconds(0.25f);

        foreach (var tileData in allMatches)
        {
            Vector2Int pos = tileData.GridPosition;
            gridData[pos.x, pos.y] = null;

            if (gridViews[pos.x, pos.y] != null)
            {
                tilePool.Release(gridViews[pos.x, pos.y]);
                gridViews[pos.x, pos.y] = null;
            }
        }
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

            hasMatches = GetAllMatches(gridData).Count > 0;

        } while (!allowMatches && hasMatches);

        tileSize = tilePrefab.GetComponent<SpriteRenderer>().bounds.size.x;
        Debug.Log($"Grid generated with size: {width}x{height}, Tile Size: {tileSize}");
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

    private List<List<TileData>> GetAllMatches(TileData[,] gridData)
    {
        var matches = new List<List<TileData>>();
        var matchedPositions = new HashSet<Vector2Int>();

        // Horizontal matches
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width - 2;)
            {
                if (gridData[x, y] == null)
                {
                    x++;
                    continue;
                }

                var matchType = gridData[x, y].Type;
                int matchLength = 1;

                for (int i = x + 1; i < width && gridData[i, y] != null && gridData[i, y].Type == matchType; i++)
                {
                    matchLength++;
                }

                if (matchLength >= 3)
                {
                    var group = new List<TileData>();
                    for (int i = x; i < x + matchLength; i++)
                    {
                        var pos = new Vector2Int(i, y);
                        if (matchedPositions.Add(pos))
                            group.Add(gridData[i, y]);
                    }
                    matches.Add(group);
                    x += matchLength;
                }
                else
                {
                    x++;
                }
            }
        }

        // Vertical matches
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height - 2;)
            {
                if (gridData[x, y] == null)
                {
                    y++;
                    continue;
                }

                var matchType = gridData[x, y].Type;
                int matchLength = 1;

                for (int i = y + 1; i < height && gridData[x, i] != null && gridData[x, i].Type == matchType; i++)
                {
                    matchLength++;
                }

                if (matchLength >= 3)
                {
                    var group = new List<TileData>();
                    for (int i = y; i < y + matchLength; i++)
                    {
                        var pos = new Vector2Int(x, i);
                        if (matchedPositions.Add(pos))
                            group.Add(gridData[x, i]);
                    }
                    matches.Add(group);
                    y += matchLength;
                }
                else
                {
                    y++;
                }
            }
        }

        return MergeIntersectingGroups(matches);
    }

    private List<List<TileData>> MergeIntersectingGroups(List<List<TileData>> groups)
    {
        var mergedGroups = new List<List<TileData>>();

        foreach (var group in groups)
        {
            bool merged = false;

            foreach (var existingGroup in mergedGroups)
            {
                if (group.Any(tile => existingGroup.Contains(tile)))
                {
                    existingGroup.AddRange(group.Where(t => !existingGroup.Contains(t)));
                    merged = true;
                    break;
                }
            }

            if (!merged)
            {
                mergedGroups.Add(new List<TileData>(group));
            }
        }

        return mergedGroups;
    }

    private TileType GetRandomTileType()
    {
        return (TileType)UnityEngine.Random.Range(0, Enum.GetValues(typeof(TileType)).Length);
    }

    private MatchShape DetermineMatchShape(List<TileData> match)
    {
        var xs = match.Select(t => t.GridPosition.x).Distinct().Count();
        var ys = match.Select(t => t.GridPosition.y).Distinct().Count();

        if (xs > 1 && ys > 1 && match.Count >= 5)
            return MatchShape.TOrL;

        if (match.Count >= 5)
            return MatchShape.FiveLine;

        if (match.Count == 4)
            return MatchShape.FourLine;

        return MatchShape.ThreeLine;
    }
}