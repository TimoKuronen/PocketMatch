using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardStateEvaluator
{
    #region Fields

    private TileData[,] gridData;
    private TileView[,] gridViews;
    private int width;
    private int height;
    private IGridController gridController;

    #endregion

    #region Constructor

    public BoardStateEvaluator(TileData[,] gridData, TileView[,] gridViews, int width, int height, IGridController controller)
    {
        this.gridData = gridData;
        this.gridViews = gridViews;
        this.width = width;
        this.height = height;
        this.gridController = controller;
    }

    #endregion

    #region Public Methods

    public PotentialMovesResult CountPotentialMoves()
    {
        HashSet<string> uniqueSwaps = new HashSet<string>();
        int swapMoves = 0;
        int powerMoves = CountPowerTiles();

        for (int x1 = 0; x1 < width; x1++)
        {
            for (int y1 = 0; y1 < height; y1++)
            {
                TileData tile1 = gridData[x1, y1];
                if (tile1 == null || tile1.State != TileState.Normal)
                    continue;

                Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

                foreach (Vector2Int dir in directions)
                {
                    Vector2Int pos2 = new Vector2Int(x1 + dir.x, y1 + dir.y);
                    if (!GridHelperMethods.IsInsideGrid(pos2, width, height))
                        continue;

                    TileData tile2 = gridData[pos2.x, pos2.y];
                    if (tile2 == null || tile2.State != TileState.Normal)
                        continue;

                    string swapKey = $"{Mathf.Min(x1, pos2.x)}-{Mathf.Min(y1, pos2.y)}_{Mathf.Max(x1, pos2.x)}-{Mathf.Max(y1, pos2.y)}";
                    if (uniqueSwaps.Contains(swapKey))
                        continue;

                    gridController.SwapTilesInData(new Vector2Int(x1, y1), pos2, tile1, tile2);
                    var matches = gridController.MatchFinder.GetMatchGroups(gridData);
                    bool createsMatch = matches.Count > 0;

                    gridController.SwapTilesInData(new Vector2Int(x1, y1), pos2, tile2, tile1);

                    if (createsMatch)
                    {
                        swapMoves++;
                        uniqueSwaps.Add(swapKey);
                    }
                }
            }
        }

        return new PotentialMovesResult(swapMoves, powerMoves);
    }

    public void ShuffleBoard()
    {
        Debug.Log("Shuffling board...");

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

        List<TileType> originalTypes = new List<TileType>();
        foreach (var tile in normalTiles)
        {
            originalTypes.Add(tile.Type);
        }

        int maxAttempts = 100;
        int attempts = 0;
        bool hasMatches = true;

        while (hasMatches && attempts < maxAttempts)
        {
            for (int i = 0; i < normalTiles.Count; i++)
            {
                normalTiles[i].Type = originalTypes[i];
            }

            for (int i = normalTiles.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (normalTiles[i].Type, normalTiles[j].Type) = (normalTiles[j].Type, normalTiles[i].Type);
            }

            for (int i = 0; i < normalTiles.Count; i++)
            {
                Vector2Int pos = tilePositions[i];
                gridData[pos.x, pos.y].Type = normalTiles[i].Type;
            }

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

        TaskRunner.Instance.StartCoroutine(AnimateShuffle(tileViews, tilePositions));
    }

    public void DebugHighlightPotentialMoves()
    {
        var moves = CountPotentialMoves();
        Debug.Log($"Potential Moves - Swaps: {moves.SwapMoveCount}, Power: {moves.PowerTileMoveCount}");

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

                    gridController.SwapTilesInData(new Vector2Int(x, y), neighborPos, current, neighbor);
                    var matches = gridController.MatchFinder.GetMatchGroups(gridData);

                    if (matches.Count > 0)
                    {
                        gridViews[x, y].GetComponent<SpriteRenderer>().color = Color.green;
                        gridViews[neighborPos.x, neighborPos.y].GetComponent<SpriteRenderer>().color = Color.green;
                    }

                    gridController.SwapTilesInData(new Vector2Int(x, y), neighborPos, neighbor, current);
                }
            }
        }
    }

    #endregion

    #region Private Methods

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

    private IEnumerator AnimateShuffle(List<TileView> tileViews, List<Vector2Int> tilePositions)
    {
        float moveDuration = 0.3f;
        List<Tweener> tweens = new();

        foreach (TileView view in tileViews)
        {
            var rect = (RectTransform)view.transform;
            rect.DOKill();

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

        foreach (var view in tileViews)
            ((RectTransform)view.transform).DOKill();

        for (int i = 0; i < tileViews.Count; i++)
        {
            Vector2Int pos = tilePositions[i];
            tileViews[i].Init(gridData[pos.x, pos.y]);
        }

        yield return new WaitForSeconds(0.05f);
        TaskRunner.Instance.StartCoroutine(gridController.MatchCycle());
    }

    #endregion
}
