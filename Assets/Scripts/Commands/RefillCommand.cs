using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RefillCommand : ICommand
{
    private readonly TileData[,] gridData;
    private readonly TileView[,] gridViews;
    private readonly int width, height;
    private readonly Func<int, int, TileView> CreateTileAt;
    private readonly Func<Vector2Int, Vector3> GridToWorldPos;
    private readonly Action TileDrop;

    public RefillCommand(TileData[,] data, TileView[,] views, int w, int h,
        Func<int, int, TileView> createFn,
        Func<Vector2Int, Vector3> toWorldFn,
        Action onDrop)
    {
        gridData = data;
        gridViews = views;
        width = w;
        height = h;
        CreateTileAt = createFn;
        GridToWorldPos = toWorldFn;
        TileDrop = onDrop;
    }

    public IEnumerator Execute()
    {
        List<Tweener> tweens = new();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (gridData[x, y] == null)
                {
                    var view = CreateTileAt(x, y);
                    var spawnPos = GridToWorldPos(new Vector2Int(x, y + 3));
                    var targetPos = GridToWorldPos(new Vector2Int(x, y));

                    view.transform.position = spawnPos;
                    view.transform.DOKill();
                    var tween = view.transform.DOMove(targetPos, 0.25f).SetEase(Ease.OutCubic);
                    tweens.Add(tween);

                    TileDrop?.Invoke();
                }
            }
        }

        if (tweens.Count > 0)
            yield return DOTween.Sequence().AppendInterval(0.25f).WaitForCompletion();
        else
            yield return null;
    }
}
