using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DropCommand : ICommand
{
    private readonly TileData[,] gridData;
    private readonly TileView[,] gridViews;
    private readonly int width, height;
    private readonly Func<Vector2Int, Vector2> GridToUIPos;
    private const float dropDuration = 0.25f;

    public DropCommand(TileData[,] data, TileView[,] views, int w, int h, Func<Vector2Int, Vector2> toUI)
    {
        gridData = data;
        gridViews = views;
        width = w;
        height = h;
        GridToUIPos = toUI;
    }

    public IEnumerator Execute()
    {
        bool movedAtLeastOne;
        do
        {
            List<Tweener> tweens = new();
            movedAtLeastOne = false;

            // 1. VERTICAL PASS (Top-Down to ensure flow)
            for (int y = 1; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (TryVerticalDrop(x, y, tweens)) movedAtLeastOne = true;
                }
            }

            // 2. DIAGONAL PASS (Only if no vertical moves happened in this iteration)
            if (!movedAtLeastOne)
            {
                // Process top-to-bottom to prevent "chain stealing"
                for (int y = 1; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (TryDiagonalSlide(x, y, tweens))
                        {
                            movedAtLeastOne = true;
                            break; // Do one slide at a time to maintain control
                        }
                    }
                    if (movedAtLeastOne) break;
                }
            }

            if (movedAtLeastOne)
                yield return DOTween.Sequence().AppendInterval(dropDuration).WaitForCompletion();

        } while (movedAtLeastOne);
    }

    private bool TryVerticalDrop(int x, int y, List<Tweener> tweens)
    {
        if (!IsCandidateToMove(x, y)) return false;
        
        int fallTo = y - 1;
        if (InBounds(x, fallTo) && IsCellEmpty(x, fallTo))
        {
            MoveTile(new Vector2Int(x, y), new Vector2Int(x, fallTo), tweens);
            return true;
        }
        return false;
    }

    private bool TryDiagonalSlide(int x, int y, List<Tweener> tweens)
    {
        if (!IsCandidateToMove(x, y)) return false;

        // Check Left then Right
        int[] directions = { -1, 1 };
        foreach (int dx in directions)
        {
            int tx = x + dx;
            int ty = y - 1;

            if (InBounds(tx, ty) && IsCellEmpty(tx, ty))
            {
                // CRITICAL: Only slide if the column ABOVE the target is BLOCKED.
                // If it's empty, we wait for a vertical drop/spawn.
                if (IsPathToSpawnerBlocked(tx, ty))
                {
                    MoveTile(new Vector2Int(x, y), new Vector2Int(tx, ty), tweens);
                    return true;
                }
            }
        }
        return false;
    }

    private bool IsPathToSpawnerBlocked(int x, int y)
    {
        // Scan from the cell up to the top.
        // If we hit a "Blocked" tile, the column cannot be filled vertically.
        // If we reach the top without hitting a wall, it's NOT blocked.
        for (int yy = y + 1; yy < height; yy++)
        {
            var tile = gridData[x, yy];
            if (tile != null)
            {
                if (tile.State == TileState.Blocked) return true;
                if (tile is DestroyableTileData dd && !dd.IsDestroyed) return true;
                // If there's a normal tile, it will eventually fall down, so the path isn't "blocked"
                if (tile.State == TileState.Normal) return false;
            }
        }
        return false; // Path to spawner is clear (or just empty)
    }

    private bool IsCandidateToMove(int x, int y)
    {
        if (!InBounds(x, y)) return false;
        var d = gridData[x, y];
        return d != null && (d.State == TileState.Normal || (d is DestroyableTileData dd && dd.IsDestroyed));
    }

    private bool IsCellEmpty(int x, int y)
    {
        if (!InBounds(x, y)) return false;
        var d = gridData[x, y];
        return d == null || d.State == TileState.Empty || (d is DestroyableTileData dd && dd.IsDestroyed);
    }

    private bool InBounds(int x, int y) => x >= 0 && x < width && y >= 0 && y < height;

    private void MoveTile(Vector2Int from, Vector2Int to, List<Tweener> tweens)
    {
        var data = gridData[from.x, from.y];
        var view = gridViews[from.x, from.y];

        gridData[to.x, to.y] = data;
        gridViews[to.x, to.y] = view;
        gridData[from.x, from.y] = new TileData(TileType.Red, from, TilePower.None, TileState.Empty);
        gridViews[from.x, from.y] = null;

        data.GridPosition = to;
        view.Data.GridPosition = to;
        tweens.Add(((RectTransform)view.transform).DOAnchorPos(GridToUIPos(to), dropDuration).SetEase(Ease.OutCubic));
    }
}