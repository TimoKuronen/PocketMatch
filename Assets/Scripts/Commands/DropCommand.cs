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
                    int downX = x;
                    int downY = y - 1;

                    // Straight down
                    if (InBounds(downX, downY) && IsCellEmpty(downX, downY))
                    {
                        MoveTile(currentPos, new Vector2Int(downX, downY), tweens);
                        moved = true;
                        continue;
                    }

                    // Diagonal left
                    int leftTx = x - 1;
                    int leftTy = y - 1;
                    if (InBounds(leftTx, leftTy) && IsCellEmpty(leftTx, leftTy) && !HasDroppableAbove(leftTx, leftTy))
                    {
                        MoveTile(currentPos, new Vector2Int(leftTx, leftTy), tweens);
                        moved = true;
                        continue;
                    }

                    // Diagonal right
                    int rightTx = x + 1;
                    int rightTy = y - 1;
                    if (InBounds(rightTx, rightTy) && IsCellEmpty(rightTx, rightTy) && !HasDroppableAbove(rightTx, rightTy))
                    {
                        MoveTile(currentPos, new Vector2Int(rightTx, rightTy), tweens);
                        moved = true;
                        continue;
                    }
                }
            }

            if (moved)
                yield return DOTween.Sequence().AppendInterval(dropDuration).WaitForCompletion();

        } while (moved && tweens.Count > 0);
    }

    private bool HasDroppableAbove(int x, int y)
    {
        if (!InBounds(x, y)) return false;

        for (int yy = y + 1; yy < height; yy++)
        {
            var above = gridData[x, yy];
            if (above == null || above.State == TileState.Empty)
                continue;

            if (above.State == TileState.Blocked)
                return false;
            if (above.State == TileState.Destroyable && above is DestroyableTileData dd && !dd.IsDestroyed)
                return false;

            if (above.State == TileState.Normal)
            {
                bool pathClear = true;
                for (int checkY = yy - 1; checkY >= y; checkY--)
                {
                    var mid = gridData[x, checkY];
                    if (mid == null) continue;

                    if (mid.State == TileState.Normal || mid.State == TileState.Blocked)
                        pathClear = false;
                    if (mid is DestroyableTileData mdd && !mdd.IsDestroyed)
                        pathClear = false;
                }
                if (pathClear) return true;
            }
        }
        return false;
    }

    private bool IsCellEmpty(int x, int y)
    {
        if (!InBounds(x, y)) return false;

        var d = gridData[x, y];
        if (d == null) return true;
        if (d.State == TileState.Empty) return true;
        if (d.State == TileState.Destroyable && d is DestroyableTileData dd && dd.IsDestroyed) return true;

        return false;
    }

    private bool IsNormalTile(int x, int y)
    {
        if (!InBounds(x, y)) return false;
        var d = gridData[x, y];
        return d != null && d.State == TileState.Normal;
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

        var rect = (RectTransform)view.transform;
        rect.DOKill();
        tweens.Add(rect.DOAnchorPos(GridToUIPos(to), dropDuration).SetEase(Ease.OutCubic));
    }
}
