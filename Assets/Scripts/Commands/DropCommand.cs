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
        do
        {
            moved = false;

            for (int y = 1; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!IsNormalTile(x, y))
                        continue;

                    int targetY = y - 1;
                    if (!InBounds(x, targetY))
                        continue;

                    // Keep falling until it hits a filled cell or bottom
                    int fallTo = y;
                    while (fallTo > 0 && IsCellEmpty(x, fallTo - 1))
                        fallTo--;

                    if (fallTo != y)
                    {
                        MoveTile(new Vector2Int(x, y), new Vector2Int(x, fallTo));
                        moved = true;
                    }
                }
            }

            if (moved)
                yield return new WaitForSeconds(dropDuration);

        } while (moved);
    }

    private void MoveTile(Vector2Int from, Vector2Int to)
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
        rect.DOAnchorPos(GridToUIPos(to), dropDuration).SetEase(Ease.OutCubic);
    }

    private bool IsNormalTile(int x, int y)
    {
        if (!InBounds(x, y)) return false;
        var d = gridData[x, y];
        return d != null && d.State == TileState.Normal;
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

    private bool InBounds(int x, int y) => x >= 0 && x < width && y >= 0 && y < height;
}