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
            tweens.Clear();

            // bottom-up: y = 1 .. height-1
            for (int y = 1; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!IsCandidateToMove(x, y))
                        continue;

                    Vector2Int currentPos = new Vector2Int(x, y);

                    // 1) Try straight down first
                    // SHOULD PRIORITIZE THESE BUT DIAGONAL SLIDES OVERPOWER - FIX!
                    int fallTo = y;
                    while (fallTo > 0 && IsCellEmpty(x, fallTo - 1))
                        fallTo--;

                    if (fallTo != y)
                    {
                        MoveTile(currentPos, new Vector2Int(x, fallTo), tweens);
                        moved = true;
                        continue;
                    }

                    // 2) Try diagonal slides (left then right) — only if target cannot be filled vertically
                    // LEFT
                    int leftTx = x - 1;
                    int leftTy = y - 1;
                    if (InBounds(leftTx, leftTy) && IsCellEmpty(leftTx, leftTy))
                    {
                        // check the target column/path: if no normal tile can drop into left target, allow slide
                        if (!HasDroppableAbove(leftTx, leftTy))
                        {
                            MoveTile(currentPos, new Vector2Int(leftTx, leftTy), tweens);
                            moved = true;
                            continue;
                        }
                    }

                    // RIGHT
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

    /// <summary>
    /// A tile is candidate to move if it's a Normal tile, or a Destroyable that has been destroyed.
    /// Intact Destroyables and Blocked cells do not move.
    /// </summary>
    private bool IsCandidateToMove(int x, int y)
    {
        if (!InBounds(x, y)) 
            return false;

        var d = gridData[x, y];

        if (d == null) 
            return false;
        if (d.State == TileState.Normal) 
            return true;
        if (d.State == TileState.Destroyable && d is DestroyableTileData dd && dd.IsDestroyed) 
            return true;

        return false;
    }

    /// <summary>
    /// Check whether any normal tile above (in same column) can drop vertically into (x,y).
    /// Used to avoid diagonal sliding when a vertical droppable exists.
    /// </summary>
    private bool HasDroppableAbove(int x, int y)
    {
        if (!InBounds(x, y)) 
            return false;

        for (int yy = y + 1; yy < height; yy++)
        {
            var above = gridData[x, yy];

            // empty space — not a source tile; continue scanning
            if (above == null || above.State == TileState.Empty)
                continue;

            // If we hit an intact blocker, the path is blocked and nothing above can reach target
            if (above.State == TileState.Blocked)
                return false;
            if (above.State == TileState.Destroyable && above is DestroyableTileData dd && !dd.IsDestroyed)
                return false;

            // If we found a normal tile, check the vertical path between that tile and the target.
            if (above.State == TileState.Normal)
            {
                bool pathClear = true;
                for (int checkY = yy - 1; checkY >= y; checkY--)
                {
                    var mid = gridData[x, checkY];

                    // If there's a normal tile or an intact blocker in the path, it's not clear
                    if (mid != null && mid.State == TileState.Normal)
                    {
                        pathClear = false;
                        break;
                    }
                    if (mid != null && mid.State == TileState.Blocked)
                    {
                        pathClear = false;
                        break;
                    }
                    if (mid is DestroyableTileData mdd && !mdd.IsDestroyed)
                    {
                        pathClear = false;
                        break;
                    }
                    // if mid is null or Empty or destroyed destroyable -> that's fine, continue
                }

                if (pathClear)
                    return true;

                // If not pathClear, continue scanning upwards — maybe a different tile can drop
                continue;
            }

            // If it's any other state, just continue scanning
        }

        // No suitable normal tile above with a clear vertical path
        return false;
    }

    private bool IsCellEmpty(int x, int y)
    {
        if (!InBounds(x, y)) 
            return false;

        var d = gridData[x, y];

        if (d == null) 
            return true;
        if (d.State == TileState.Empty) 
            return true;
        if (d.State == TileState.Destroyable && d is DestroyableTileData dd && dd.IsDestroyed) 
            return true;

        return false;
    }

    private bool InBounds(int x, int y) => x >= 0 && x < width && y >= 0 && y < height;

    private void MoveTile(Vector2Int from, Vector2Int to, List<Tweener> tweens)
    {
        var data = gridData[from.x, from.y];
        var view = gridViews[from.x, from.y];

        if (view == null || data == null)
        {
            Debug.LogError(data + " is null when trying to move to position " + from.ToString());
            Debug.LogError(view + " is null");
            Debug.Break();
        }
        gridData[to.x, to.y] = data;
        gridViews[to.x, to.y] = view;

        gridData[from.x, from.y] = new TileData(TileType.Red, from, TilePower.None, TileState.Empty);
        gridViews[from.x, from.y] = null;

        data.GridPosition = to;
        view.Data.GridPosition = to;

        RectTransform rect = (RectTransform)view.transform;
        rect.DOKill();
        tweens.Add(rect.DOAnchorPos(GridToUIPos(to), dropDuration).SetEase(Ease.OutCubic));
    }
}
