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

    public DropCommand(TileData[,] data, TileView[,] views, int w, int h, System.Func<Vector2Int, Vector3> toWorld)
    {
        gridData = data;
        gridViews = views;
        width = w;
        height = h;
        GridToWorldPos = toWorld;
    }

    public IEnumerator Execute()
    {
        var tweens = new List<Tweener>();
        bool moved;
        int safety = width * height * 8;

        do
        {
            moved = false;

            // --- PHASE 1: vertical drops ---
            for (int y = 1; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!IsNormalTile(x, y)) 
                        continue;

                    if (IsCellEmpty(x, y - 1))
                    {
                        MoveTile(x, y, x, y - 1, tweens);
                        moved = true;
                    }
                }
            }

            // --- PHASE 2: diagonal flow ---
            for (int y = 1; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!IsNormalTile(x, y)) 
                        continue;

                    // only try diagonal if below is NOT empty
                    if (!IsCellEmpty(x, y - 1))
                    {
                        // slide left
                        if (IsInside(x - 1, y - 1) &&
                            IsCellEmpty(x - 1, y - 1) &&
                            !IsRefillableCell(x - 1, y))
                        {
                            MoveTile(x, y, x - 1, y - 1, tweens);
                            moved = true;
                            continue;
                        }

                        // slide right
                        if (IsInside(x + 1, y - 1) &&
                            IsCellEmpty(x + 1, y - 1) &&
                            !IsRefillableCell(x + 1, y))
                        {
                            MoveTile(x, y, x + 1, y - 1, tweens);
                            moved = true;
                            continue;
                        }
                    }
                }
            }

            safety--;
        } while (moved && safety > 0);

        if (tweens.Count > 0)
            yield return DOTween.Sequence().AppendInterval(0.2f).WaitForCompletion();
    }

    private bool IsInside(int x, int y) => x >= 0 && x < width && y >= 0 && y < height;

    private bool IsNormalTile(int x, int y)
    {
        if (!IsInside(x, y)) 
            return false;
        
        var d = gridData[x, y];
        return d != null && d.State == TileState.Normal && gridViews[x, y] != null;
    }

    /// <summary>
    /// A cell is empty if it has no blocking view and data is empty/null or a destroyed destroyable.
    /// We check both data and view so we never "fall into" a blocked/breakable.
    /// </summary>
    private bool IsCellEmpty(int x, int y)
    {
        if (!IsInside(x, y)) return false;

        var view = gridViews[x, y];
        var data = gridData[x, y];

        // If there's any view that represents a tile/obstacle, it's not empty.
        if (view != null && view.Data != null && view.Data.State != TileState.Empty)
            return false;

        // No view or view is just an empty slot; check data.
        if (data == null || data.State == TileState.Empty)
            return true;

        if (data.State == TileState.Destroyable && data is DestroyableTileData destroyable)
            return destroyable.IsDestroyed;

        return false;
    }

    /// <summary>
    /// Whether a cell could accept a new tile (used for reasoning about "will target be filled from above").
    /// </summary>
    private bool IsRefillableCell(int x, int y)
    {
        if (!IsInside(x, y)) 
            return false;

        var data = gridData[x, y];
        var view = gridViews[x, y];

        // If there's a solid view (normal/blocked/breakable not destroyed), it's not refillable.
        if (view != null && view.Data != null && view.Data.State != TileState.Empty)
            return false;

        if (data == null || data.State == TileState.Empty)
            return true;

        if (data.State == TileState.Destroyable && data is DestroyableTileData destroyable)
            return destroyable.IsDestroyed;

        return false;
    }

    private void MoveTile(int fromX, int fromY, int toX, int toY, List<Tweener> tweens)
    {
        var data = gridData[fromX, fromY];
        var view = gridViews[fromX, fromY];

        // write new logical position
        var newPos = new Vector2Int(toX, toY);
        data.GridPosition = newPos;
        view.Data.GridPosition = newPos;

        gridData[toX, toY] = data;
        gridViews[toX, toY] = view;

        // mark source as empty
        gridData[fromX, fromY] = new TileData(TileType.Red, new Vector2Int(fromX, fromY), TilePower.None, TileState.Empty);
        gridViews[fromX, fromY] = null;

        // tween
        view.transform.DOKill();
        tweens.Add(view.transform.DOMove(GridToWorldPos(newPos), 0.2f).SetEase(Ease.OutCubic));
    }
}
