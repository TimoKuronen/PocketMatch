using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravityCommand : ICommand
{
    private readonly TileData[,] gridData;
    private readonly TileView[,] gridViews;
    private readonly int width, height;
    private readonly Func<Vector2Int, Vector2> GridToUIPos;
    private readonly Func<int, int, TileView> CreateTileAt;
    private readonly MapData mapData;
    private const float stepDuration = 0.20f;

    public GravityCommand(TileData[,] data, TileView[,] views, int w, int h, Func<Vector2Int, Vector2> toUI, Func<int, int, TileView> createFn, MapData mapData)
    {
        gridData = data;
        gridViews = views;
        width = w;
        height = h;
        GridToUIPos = toUI;
        CreateTileAt = createFn;
        this.mapData = mapData;
    }

    public IEnumerator Execute()
    {
        bool boardChanged;
        do
        {
            boardChanged = false;
            List<Tweener> tweens = new();

            // 1. VERTICAL FALL (Always check this for the whole board first)
            for (int y = 1; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (GridHelperMethods.IsMovable(gridData, x, y, width, height) && GridHelperMethods.IsCellEmpty(gridData, x, y - 1, width, height))
                    {
                        MoveTile(new Vector2Int(x, y), new Vector2Int(x, y - 1), tweens);
                        boardChanged = true;
                    }
                }
            }

            // 2. REFILL (Always check if spawners can drop a tile vertically first)
            if (!boardChanged)
            {
                for (int x = 0; x < width; x++)
                {
                    if (GridHelperMethods.IsCellEmpty(gridData, x, height - 1, width, height) && !IsPathToSpawnerBlocked(x, height - 1))
                    {
                        SpawnTile(x, height - 1, tweens);
                        boardChanged = true;
                    }
                }
            }

            // 3. THE WATERFALL CASCADE (Only if nothing can move or spawn vertically)
            if (!boardChanged)
            {
                for (int y = height - 1; y >= 0; y--)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (!GridHelperMethods.IsMovable(gridData, x, y, width, height)) continue;

                        if (TryWaterfallSlide(x, y, tweens))
                        {
                            boardChanged = true;
                            goto EndStep;
                        }
                    }
                }
            }

        EndStep:
            if (boardChanged)
                yield return DOTween.Sequence().AppendInterval(stepDuration).WaitForCompletion();

        } while (boardChanged);
    }

    private bool TryWaterfallSlide(int x, int y, List<Tweener> tweens)
    {
        int[] sideOffsets = { -1, 1 };
        foreach (int dx in sideOffsets)
        {
            int tx = x + dx;
            int ty = y - 1;

            if (GridHelperMethods.IsInBounds(width, height, tx, ty) && GridHelperMethods.IsCellEmpty(gridData, tx, ty, width, height))
            {
                if (IsPathToSpawnerBlocked(tx, ty))
                {
                    MoveTile(new Vector2Int(x, y), new Vector2Int(tx, ty), tweens);
                    return true;
                }
            }
        }
        return false;
    }

    private bool IsPathToSpawnerBlocked(int x, int y)
    {
        for (int checkY = y + 1; checkY < height; checkY++)
        {
            var d = gridData[x, checkY];
            if (d != null && (d.State == TileState.Blocked || (d is DestroyableTileData dd && !dd.IsDestroyed)))
                return true;
        }
        return false;
    }

    private void SpawnTile(int x, int y, List<Tweener> tweens)
    {
        if (gridData[x, y] == null)
        {
            gridData[x, y] = new TileData(GridHelperMethods.GetRandomTileType(mapData), new Vector2Int(x, y), TilePower.None, TileState.Empty);
        }

        var view = CreateTileAt(x, y);
        if (view == null) return;

        var rect = GridHelperMethods.GetRectTransform(view);
        rect.anchoredPosition = GridToUIPos(new Vector2Int(x, height));
        tweens.Add(rect.DOAnchorPos(GridToUIPos(new Vector2Int(x, y)), stepDuration).SetEase(Ease.OutCubic));
    }

    private void MoveTile(Vector2Int from, Vector2Int to, List<Tweener> tweens)
    {
        var data = gridData[from.x, from.y];
        var view = gridViews[from.x, from.y];

        gridData[to.x, to.y] = data;
        gridViews[to.x, to.y] = view;

        gridData[from.x, from.y] = GridHelperMethods.CreateEmptyTile(from);
        gridViews[from.x, from.y] = null;

        GridHelperMethods.UpdateTilePosition(data, view, to);

        var rect = GridHelperMethods.GetRectTransform(view);
        tweens.Add(rect.DOAnchorPos(GridToUIPos(to), stepDuration).SetEase(Ease.OutCubic));
    }
}