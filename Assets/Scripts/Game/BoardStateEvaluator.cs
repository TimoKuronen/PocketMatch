using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardStateEvaluator
{
    private TileData[,] gridData;
    private TileView[,] gridViews;

    private int width;
    private int height;

    private GridController gridController;

    public BoardStateEvaluator(TileData[,] gridData, TileView[,] gridViews, int width, int height, GridController controller)
    {
        this.gridData = gridData;
        this.gridViews = gridViews;
        this.width = width;
        this.height = height;
        this.gridController = controller;
    }

    public PotentialMovesResult CountPotentialMoves()
    {
        HashSet<string> uniqueSwaps = new HashSet<string>(); // Tracks "x1-y1_x2-y2" pairs
        int swapMoves = 0;
        int powerMoves = CountPowerTiles(); // Count power tiles separately

        // Check ALL possible swaps (no direction bias)
        for (int x1 = 0; x1 < width; x1++)
        {
            for (int y1 = 0; y1 < height; y1++)
            {
                TileData tile1 = gridData[x1, y1];
                if (tile1 == null || tile1.State != TileState.Normal)
                    continue;

                // Check all 4 directions
                Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

                foreach (Vector2Int dir in directions)
                {
                    Vector2Int pos2 = new Vector2Int(x1 + dir.x, y1 + dir.y);
                    if (!GridHelperMethods.IsInsideGrid(pos2, width, height))
                        continue;

                    TileData tile2 = gridData[pos2.x, pos2.y];
                    if (tile2 == null || tile2.State != TileState.Normal)
                        continue;

                    // Create a unique key for this pair (order-independent)
                    string swapKey = $"{Mathf.Min(x1, pos2.x)}-{Mathf.Min(y1, pos2.y)}_{Mathf.Max(x1, pos2.x)}-{Mathf.Max(y1, pos2.y)}";
                    if (uniqueSwaps.Contains(swapKey))
                        continue;

                    // Simulate swap
                    gridController.SwapTilesInData(new Vector2Int(x1, y1), pos2, tile1, tile2);
                    var matches = gridController.MatchFinder.GetMatchGroups(gridData);
                    bool createsMatch = matches.Count > 0;

                    // Undo swap
                    gridController.SwapTilesInData(new Vector2Int(x1, y1), pos2, tile2, tile1);

                    if (createsMatch)
                    {
                        swapMoves++;
                        uniqueSwaps.Add(swapKey);
                        //Debug.Log($"Valid move: ({x1},{y1}) <-> ({pos2.x},{pos2.y})");
                    }
                }
            }
        }

        return new PotentialMovesResult(swapMoves, powerMoves);
    }

    /// <summary>
    /// Counts all power tiles on the board (each is a potential move).
    /// </summary>
    private int CountPowerTiles()
    {
        int count = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                TileData tile = gridData[x, y];
                if (tile != null && tile.Power != TilePower.None)
                {
                    count++;
                }
            }
        }
        return count;
    }
    public void ShuffleBoard()
    {
        Debug.Log("Shuffling board...");

        // 1. Collect all normal tiles
        List<TileData> normalTiles = new List<TileData>();
        List<TileView> tileViews = new List<TileView>();
        List<Vector2Int> tilePositions = new List<Vector2Int>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                TileData tile = gridData[x, y];
                if (tile != null && tile.State == TileState.Normal)
                {
                    normalTiles.Add(tile);
                    tileViews.Add(gridViews[x, y]);
                    tilePositions.Add(new Vector2Int(x, y));
                }
            }
        }

        // 2. Create a backup of original types for safety
        List<TileType> originalTypes = new List<TileType>();
        foreach (var tile in normalTiles)
        {
            originalTypes.Add(tile.Type);
        }

        // 3. Shuffle logic with match prevention
        int maxAttempts = 100;
        int attempts = 0;
        bool hasMatches = true;

        while (hasMatches && attempts < maxAttempts)
        {
            // Reset to original types before each attempt
            for (int i = 0; i < normalTiles.Count; i++)
            {
                normalTiles[i].Type = originalTypes[i];
            }

            // Shuffle the types
            for (int i = normalTiles.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (normalTiles[i].Type, normalTiles[j].Type) = (normalTiles[j].Type, normalTiles[i].Type);
            }

            // Apply shuffled types to grid
            for (int i = 0; i < normalTiles.Count; i++)
            {
                Vector2Int pos = tilePositions[i];
                gridData[pos.x, pos.y].Type = normalTiles[i].Type;
            }

            // Check for matches
            hasMatches = gridController.MatchFinder.GetMatchGroups(gridData).Count > 0;
            attempts++;
        }

        if (attempts >= maxAttempts)
        {
            Debug.LogWarning($"Could not shuffle without matches after {maxAttempts} attempts. Using last arrangement.");
        }
        else
        {
            Debug.Log($"Successfully shuffled without matches in {attempts} attempts.");
        }

        // 5. Animate the shuffle with DOTween
        CoroutineMonoBehavior.Instance.StartCoroutine(AnimateShuffle(tileViews, tilePositions));
    }

    private IEnumerator AnimateShuffle(List<TileView> tileViews, List<Vector2Int> tilePositions)
    {
        float moveDuration = 0.3f;
        List<Tweener> tweens = new();

        foreach (TileView view in tileViews)
        {
            var rect = (RectTransform)view.transform;
            rect.DOKill(); // kill existing tweens

            Vector2 currentPos = rect.anchoredPosition;
            Vector2 randomOffset = new Vector2(
                UnityEngine.Random.Range(-30f, 30f),
                UnityEngine.Random.Range(-30f, 30f)
            );

            Tweener t = rect.DOAnchorPos(currentPos + randomOffset, moveDuration)
                .SetEase(Ease.InOutQuad)
                .SetLoops(2, LoopType.Yoyo);
            tweens.Add(t);
        }

        yield return new WaitForSeconds(moveDuration * 2);

        // Ensure all tweens are cleared
        foreach (var view in tileViews)
            ((RectTransform)view.transform).DOKill();

        // Re-init visuals
        for (int i = 0; i < tileViews.Count; i++)
        {
            Vector2Int pos = tilePositions[i];
            tileViews[i].Init(gridData[pos.x, pos.y]);
        }

        // Resume normal matching AFTER animations are fully done
        yield return new WaitForSeconds(0.05f);
        CoroutineMonoBehavior.Instance.StartCoroutine(gridController.MatchCycle());
    }

    /// <summary>
    /// DEBUG: Highlights all tiles involved in potential moves.
    /// </summary>
    public void DebugHighlightPotentialMoves()
    {
        var moves = CountPotentialMoves();
        Debug.Log($"Potential Moves - Swaps: {moves.SwapMoveCount}, Power: {moves.PowerTileMoveCount}");

        // Highlight power tiles
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                TileData tile = gridData[x, y];
                if (tile != null && tile.Power != TilePower.None)
                {
                    gridViews[x, y].GetComponent<SpriteRenderer>().color = Color.yellow;
                }
            }
        }

        // Highlight swap-based moves (simulate swaps and check matches)
        HashSet<string> checkedSwaps = new HashSet<string>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                TileData current = gridData[x, y];
                if (current == null || current.State != TileState.Normal)
                    continue;

                Vector2Int[] directions = new Vector2Int[] { Vector2Int.right, Vector2Int.down };
                foreach (Vector2Int dir in directions)
                {
                    Vector2Int neighborPos = new Vector2Int(x, y) + dir;
                    if (!GridHelperMethods.IsInsideGrid(neighborPos, width, height))
                        continue;

                    string swapKey = $"{Mathf.Min(x, neighborPos.x)}-{Mathf.Min(y, neighborPos.y)}";
                    if (checkedSwaps.Contains(swapKey))
                        continue;

                    checkedSwaps.Add(swapKey);
                    TileData neighbor = gridData[neighborPos.x, neighborPos.y];
                    if (neighbor == null || neighbor.State != TileState.Normal)
                        continue;

                    // Simulate swap
                    gridController.SwapTilesInData(new Vector2Int(x, y), neighborPos, current, neighbor);
                    var matches = gridController.MatchFinder.GetMatchGroups(gridData);

                    if (matches.Count > 0)
                    {
                        gridViews[x, y].GetComponent<SpriteRenderer>().color = Color.green;
                        gridViews[neighborPos.x, neighborPos.y].GetComponent<SpriteRenderer>().color = Color.green;
                    }

                    // Undo swap
                    gridController.SwapTilesInData(new Vector2Int(x, y), neighborPos, neighbor, current);
                }
            }
        }
    }
}
