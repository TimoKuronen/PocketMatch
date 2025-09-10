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
    private readonly Func<Vector2Int, Vector3> GridToWorldPos;

    private const float dropDuration = 0.25f;

    public DropCommand(TileData[,] data, TileView[,] views, int w, int h, Func<Vector2Int, Vector3> toWorld)
    {
        gridData = data;
        gridViews = views;
        width = w;
        height = h;
        GridToWorldPos = toWorld;
    }

    public IEnumerator Execute()
    {
        bool moved;
        List<Tweener> tweens = new();

        do
        {
            moved = false;

            for (int y = 1; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!IsNormalTile(x, y))
                        continue;

                    Vector2Int currentPos = new(x, y);

                    // --- 1) Straight vertical drop has priority ---
                    int downX = x;
                    int downY = y - 1;
                    if (IsCellEmpty(downX, downY))
                    {

                        MoveTile(currentPos, new Vector2Int(downX, downY), tweens);
                        moved = true;
                        continue;
                    }

                    // --- 2) Diagonal cascade: attempt to slide into neighbor column's empty cell ---
                    int leftTx = x - 1;
                    int leftTy = y - 1;

                    if (InBounds(leftTx, leftTy) && IsCellEmpty(leftTx, leftTy))
                    {
                        // if the target column cannot be filled vertically, allow a slide from current tile
                        if (!HasDroppableAbove(leftTx, leftTy))
                        {        
                            MoveTile(currentPos, new Vector2Int(leftTx, leftTy), tweens);
                            moved = true;

                            continue;
                        }
                    }

                    // Right diagonal target:
                    int rightTx = x + 1;
                    int rightTy = y - 1;

                    if (InBounds(rightTx, rightTy) && IsCellEmpty(rightTx, rightTy))
                    {
                        if (!HasDroppableAbove(rightTx, rightTy))
                        {                        
                            MoveTile(currentPos, new Vector2Int(rightTx, rightTy), tweens);
                            moved = true;

                            continue;
                        }
                    }
                }
            }
            if (moved)
                yield return DOTween.Sequence().AppendInterval(dropDuration).WaitForCompletion();

        } while (moved && tweens.Count > 0);
    }

    private bool HasDroppableAbove(int x, int y)
    {
        for (int yy = y + 1; yy < height; yy++)
        {
            var above = gridData[x, yy];
            if (above == null) 
                continue;
            if (above.State == TileState.Empty)
                return true; // found an empty tile
            if (above.State == TileState.Normal)
                return true; // found a normal tile that could fall down
            if (above.State == TileState.Blocked)
                return false; // blocked, nothing above can reach here
            if (above is DestroyableTileData d && !d.IsDestroyed)
            {
                return false; // blocked by undestroyed destroyable
            }
        }
        
        return false;
    }

    private bool IsCellEmpty(int x, int y)
    {
        if (!InBounds(x, y))
        {
            Debug.LogWarning($"IsCellEmpty: Out of bounds {x},{y}");
            return false;
        }

        var data = gridData[x, y];

        if (data == null || data.State == TileState.Empty)
            return true;
        if (data.State == TileState.Destroyable && data is DestroyableTileData d && d.IsDestroyed)
            return true;

        return false;
    }

    private void MoveTile(Vector2Int from, Vector2Int to, List<Tweener> tweens)
    {
        var data = gridData[from.x, from.y];
        var view = gridViews[from.x, from.y];


        gridData[to.x, to.y] = data;
        gridViews[to.x, to.y] = view;

        gridData[from.x, from.y] = new TileData(TileType.Red, new Vector2Int(from.x, from.y), TilePower.None, TileState.Empty);
        gridViews[from.x, from.y] = null;

        data.GridPosition = to;
        view.Data.GridPosition = to;

        view.transform.DOKill();
        tweens.Add(view.transform.DOMove(GridToWorldPos(to), dropDuration).SetEase(Ease.OutCubic));
    }

    private bool IsNormalTile(int x, int y)
    {
        if (!InBounds(x, y))
            return false;

        return gridData[x, y] is { State: TileState.Normal };
    }

    private bool InBounds(int x, int y) => x >= 0 && x < width && y >= 0 && y < height;
}