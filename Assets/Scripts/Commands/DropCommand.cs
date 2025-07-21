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
        List<Tweener> tweens = new();

        for (int x = 0; x < width; x++)
        {
            int writeY = 0;

            for (int readY = 0; readY < height; readY++)
            {
                var data = gridData[x, readY];
                var view = gridViews[x, readY];

                // Skip empty or blocked
                if (data == null || data.State != TileState.Normal) 
                    continue;

                gridData[x, readY] = null;
                gridViews[x, readY] = null;

                // Find next free, non-blocked slot below
                while (writeY < readY && (gridData[x, writeY]?.State == TileState.Blocked))
                    writeY++;

                data.GridPosition = new Vector2Int(x, writeY);
                view.Data.GridPosition = data.GridPosition;

                gridData[x, writeY] = data;
                gridViews[x, writeY] = view;

                view.transform.DOKill();
                tweens.Add(view.transform.DOMove(GridToWorldPos(data.GridPosition), 0.2f).SetEase(Ease.OutCubic));

                writeY++;
            }

            // Clear leftover
            for (int y = writeY; y < height; y++)
            {
                if (gridData[x, y] != null && gridData[x, y].State == TileState.Normal)
                {
                    gridData[x, y] = null;
                    gridViews[x, y] = null;
                }
            }
        }

        if (tweens.Count > 0)
            yield return DOTween.Sequence().AppendInterval(0.2f).WaitForCompletion();
        else
            yield return null;
    }
}