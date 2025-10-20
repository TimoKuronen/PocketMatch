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
    private readonly Func<Vector2Int, Vector2> GridToUIPos;
    private readonly Action TileDrop;

    private const float refillDuration = 0.25f;

    public RefillCommand(TileData[,] data, TileView[,] views, int w, int h,
        Func<int, int, TileView> createFn,
        Func<Vector2Int, Vector2> toUIFn,
        Action onDrop)
    {
        gridData = data;
        gridViews = views;
        width = w;
        height = h;
        CreateTileAt = createFn;
        GridToUIPos = toUIFn;
        TileDrop = onDrop;
    }

    public IEnumerator Execute()
    {
        List<Tweener> tweens = new();

        for (int x = 0; x < width; x++)
        {
            for (int y = height - 1; y >= 0; y--)
            {
                if (!IsRefillable(x, y) || gridViews[x, y] != null)
                    continue;

                if (!HasClearPathAbove(x, y))
                    continue;

                var view = CreateTileAt(x, y);
                var rect = (RectTransform)view.transform;

                // spawn well above top row so the drop looks natural
                var spawnPos = GridToUIPos(new Vector2Int(x, height + 2));
                var targetPos = GridToUIPos(new Vector2Int(x, y));

                rect.anchoredPosition = spawnPos;
                rect.DOKill();
                tweens.Add(rect.DOAnchorPos(targetPos, refillDuration).SetEase(Ease.OutCubic));

                TileDrop?.Invoke();
            }
        }

        if (tweens.Count > 0)
            yield return DOTween.Sequence().AppendInterval(refillDuration).WaitForCompletion();
    }

    private bool IsRefillable(int x, int y)
    {
        if (!InBounds(x, y)) 
            return false;

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
