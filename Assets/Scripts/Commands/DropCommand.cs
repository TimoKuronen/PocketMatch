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

            for (int y = 1; y < height; y++) // skip bottom row
            {
                for (int x = 0; x < width; x++)
                {
                    if (!IsNormalTile(x, y))
                        continue;

                    Vector2Int currentPos = new(x, y);

                    // --- Try straight down first ---
                    if (IsCellEmpty(x, y - 1))
                    {
                        MoveTile(currentPos, new Vector2Int(x, y - 1), tweens);
                        moved = true;
                        continue;
                    }

                    // --- Try diagonal left ---
                    if (IsCellEmpty(x - 1, y - 1) && IsCellEmpty(x - 1, y))
                    {
                        MoveTile(currentPos, new Vector2Int(x - 1, y - 1), tweens);
                        moved = true;
                        continue;
                    }

                    // --- Try diagonal right ---
                    if (IsCellEmpty(x + 1, y - 1) && IsCellEmpty(x + 1, y))
                    {
                        MoveTile(currentPos, new Vector2Int(x + 1, y - 1), tweens);
                        moved = true;
                        continue;
                    }
                }
            }

            if (moved)
                yield return DOTween.Sequence().AppendInterval(dropDuration).WaitForCompletion();

        } while (moved && tweens.Count > 0);
    }

    private void MoveTile(Vector2Int from, Vector2Int to, List<Tweener> tweens)
    {
        var data = gridData[from.x, from.y];
        var view = gridViews[from.x, from.y];

        gridData[to.x, to.y] = data;
        gridViews[to.x, to.y] = view;

        gridData[from.x, from.y] = null;
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

    private bool IsCellEmpty(int x, int y)
    {
        if (!InBounds(x, y)) 
            return false;

        var data = gridData[x, y];

        if (data == null || data.State == TileState.Empty) 
            return true;
        if (data.State == TileState.Destroyable && data is DestroyableTileData d && d.IsDestroyed)
            return true;

        return false;
    }

    private bool InBounds(int x, int y) => x >= 0 && x < width && y >= 0 && y < height;
}