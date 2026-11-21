using DG.Tweening;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class BoardStateEvaluator
{
    private TileData[,] gridData;
    private TileView[,] gridViews;
    private int width;
    private int height;
    private IGridController gridController;

    private struct SwapKey : System.IEquatable<SwapKey>
    {
        public int x1, y1, x2, y2;

        public SwapKey(int x1, int y1, int x2, int y2)
        {
            this.x1 = x1;
            this.y1 = y1;
            this.x2 = x2;
            this.y2 = y2;
        }

        public bool Equals(SwapKey other)
        {
            return x1 == other.x1 && y1 == other.y1 && x2 == other.x2 && y2 == other.y2;
        }

        public override int GetHashCode()
        {
            return x1.GetHashCode() ^ (y1.GetHashCode() << 2) ^ (x2.GetHashCode() >> 2) ^ (y2.GetHashCode() << 4);
        }
    }

    public BoardStateEvaluator(TileData[,] gridData, TileView[,] gridViews, int width, int height, IGridController controller)
    {
        this.gridData = gridData;
        this.gridViews = gridViews;
        this.width = width;
        this.height = height;
        this.gridController = controller;
    }

    public PotentialMovesResult CountPotentialMoves()
    {
        HashSet<SwapKey> uniqueSwaps = new HashSet<SwapKey>();
        int swapMoves = 0;
        int powerMoves = CountPowerTiles();

        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        for (int x1 = 0; x1 < width; x1++)
        {
            for (int y1 = 0; y1 < height; y1++)
            {
                TileData tile1 = gridData[x1, y1];
                if (tile1 == null || tile1.State != TileState.Normal)
                    continue;

                for (int d = 0; d < directions.Length; d++)
                {
                    Vector2Int dir = directions[d];
                    Vector2Int pos2 = new Vector2Int(x1 + dir.x, y1 + dir.y);
                    if (!GridHelperMethods.IsInsideGrid(pos2, width, height))
                        continue;

                    TileData tile2 = gridData[pos2.x, pos2.y];
                    if (tile2 == null || tile2.State != TileState.Normal)
                        continue;

                    SwapKey swapKey = new SwapKey(Mathf.Min(x1, pos2.x), Mathf.Min(y1, pos2.y), Mathf.Max(x1, pos2.x), Mathf.Max(y1, pos2.y));
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

        List<TileView> tileViews = new List<TileView>();
        List<Vector2Int> tilePositions = new List<Vector2Int>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                TileData tile = gridData[x, y];
                if (tile != null && tile.State == TileState.Normal)
                {
                    tileViews.Add(gridViews[x, y]);
                    tilePositions.Add(new Vector2Int(x, y));
                }
            }
        }

        bool success = GridHelperMethods.ShuffleTypesUntilPlayable(
            gridData, width, height, gridController.MatchFinder, null, 150);

        if (!success)
        {
            Debug.LogWarning($"Could not shuffle to no matches + possible moves after 150 attempts. Using last arrangement.");
        }
        else
        {
            Debug.Log($"Shuffled to playable board (no matches, has moves).");
        }

        AnimateShuffleAsync(tileViews, tilePositions).Forget();
    }

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

    private async UniTask AnimateShuffleAsync(List<TileView> tileViews, List<Vector2Int> tilePositions)
    {
        var token = (gridController as MonoBehaviour)?.GetCancellationTokenOnDestroy() ?? CancellationToken.None;
        float moveDuration = 0.3f;

        foreach (TileView view in tileViews)
        {
            var rect = (RectTransform)view.transform;
            rect.DOKill();

            Vector2 currentPos = rect.anchoredPosition;
            Vector2 randomOffset = new Vector2(
                UnityEngine.Random.Range(-30f, 30f),
                UnityEngine.Random.Range(-30f, 30f)
            );

            rect.DOAnchorPos(currentPos + randomOffset, moveDuration)
                .SetEase(Ease.InOutQuad)
                .SetLoops(2, LoopType.Yoyo);
        }

        await UniTask.Delay(System.TimeSpan.FromSeconds(moveDuration * 2), cancellationToken: token);

        foreach (var view in tileViews)
            ((RectTransform)view.transform).DOKill();

        for (int i = 0; i < tileViews.Count; i++)
        {
            Vector2Int pos = tilePositions[i];
            tileViews[i].Init(gridData[pos.x, pos.y]);
        }

        await UniTask.Delay(System.TimeSpan.FromSeconds(0.05f), cancellationToken: token);
        await gridController.MatchCycleAsync();
    }
}
