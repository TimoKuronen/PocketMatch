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
            List<(TileData data, TileView view)> buffer = new();

            // Collect all tiles in this column
            for (int y = 0; y < height; y++)
            {
                if (gridData[x, y] != null)
                {
                    buffer.Add((gridData[x, y], gridViews[x, y]));
                    gridData[x, y] = null;
                    gridViews[x, y] = null;
                }
            }

            // Assign all at once: logic update
            for (int y = 0; y < buffer.Count; y++)
            {
                var (data, view) = buffer[y];
                var newPos = new Vector2Int(x, y);

                data.GridPosition = newPos;
                view.Data.GridPosition = newPos;

                gridData[x, y] = data;
                gridViews[x, y] = view;

                view.transform.DOKill();
                var tween = view.transform.DOMove(GridToWorldPos(newPos), 0.2f).SetEase(Ease.OutCubic);
                tweens.Add(tween);
            }

            // Clear the rest
            for (int y = buffer.Count; y < height; y++)
            {
                gridData[x, y] = null;
                gridViews[x, y] = null;
            }
        }

        // Wait for all tweens to finish
        if (tweens.Count > 0)
            yield return DOTween.Sequence().AppendInterval(0.2f).WaitForCompletion();
        else
            yield return null;
    }
}
