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

    private const float refillDuration = 0.25f;

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
            for (int y = height - 1; y >= 0; y--) // top to bottom
            {
                if (!IsRefillable(x, y) || gridViews[x, y] != null)
                    continue;

                // Only spawn if path from top is clear (no obstacles above this cell)
                if (!HasClearPathAbove(x, y))
                    continue;

                var view = CreateTileAt(x, y);
                var spawnPos = GridToWorldPos(new Vector2Int(x, height + 2));
                var targetPos = GridToWorldPos(new Vector2Int(x, y));

                view.transform.position = spawnPos;
                view.transform.DOKill();
                tweens.Add(view.transform.DOMove(targetPos, refillDuration).SetEase(Ease.OutCubic));

                TileDrop?.Invoke();
            }
        }

        yield return tweens.Count > 0
            ? DOTween.Sequence().AppendInterval(refillDuration).WaitForCompletion()
            : null;
    }

    private bool IsRefillable(int x, int y)
    {
        if (!InBounds(x, y)) return false;

        var data = gridData[x, y];
        if (data == null || data.State == TileState.Empty)
            return true;

        if (data.State == TileState.Destroyable && data is DestroyableTileData destroyable)
            return destroyable.IsDestroyed;

        return false;
    }

    private bool HasClearPathAbove(int x, int y)
    {
        for (int checkY = y + 1; checkY < height; checkY++)
        {
            var data = gridData[x, checkY];
            if (data is { State: TileState.Blocked })
                return false;
            if (data.State == TileState.Destroyable && data is DestroyableTileData destroyable && !destroyable.IsDestroyed)
                return false;
        }
        return true;
    }

    private bool InBounds(int x, int y) => x >= 0 && x < width && y >= 0 && y < height;
}