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
    private const float stepDuration = 0.20f;
    MapData mapData;

    public GravityCommand(TileData[,] data, TileView[,] views, int w, int h,
                          Func<Vector2Int, Vector2> toUI, Func<int, int, TileView> createFn, MapData mapData)
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
                    if (IsMovable(x, y) && IsEmpty(x, y - 1))
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
                    if (IsEmpty(x, height - 1) && !IsPathToSpawnerBlocked(x, height - 1))
                    {
                        SpawnTile(x, height - 1, tweens);
                        boardChanged = true;
                        // Important: We only spawn one to let it fall vertically 
                        // in the next loop iteration before we consider cascading.
                    }
                }
            }

            // 3. THE WATERFALL CASCADE (Only if nothing can move or spawn vertically)
            if (!boardChanged)
            {
                // CRITICAL: We scan from top (height-1) down to bottom (0).
                // This ensures that the tile at (1,3) is the FIRST one 
                // that gets to look at the hole in Column 0.
                for (int y = height - 1; y >= 0; y--)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (!IsMovable(x, y)) continue;

                        // A tile at (x,y) looks at its diagonal neighbors below it.
                        if (TryWaterfallSlide(x, y, tweens))
                        {
                            boardChanged = true;
                            // WE STOP IMMEDIATELY. This forces the loop to restart.
                            // This allows the spawner to fill the gap left by this tile 
                            // before any tiles below it try to slide.
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
        // A tile at (x,y) checks if (x-1, y-1) or (x+1, y-1) are empty AND blocked from above.
        int[] sideOffsets = { -1, 1 };
        foreach (int dx in sideOffsets)
        {
            int tx = x + dx;
            int ty = y - 1;

            if (InBounds(tx, ty) && IsEmpty(tx, ty))
            {
                // Only slide if the target's column is permanently blocked from the top.
                // In your example: Column 0 is blocked at (0,3).
                if (IsPathToSpawnerBlocked(tx, ty))
                {
                    MoveTile(new Vector2Int(x, y), new Vector2Int(tx, ty), tweens);
                    Debug.Break();
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
        // Ensure gridData exists so CreateTileAt doesn't fail
        if (gridData[x, y] == null)
        {
            gridData[x, y] = new TileData(GetRandomTileType(), new Vector2Int(x, y), TilePower.None, TileState.Empty);
        }

        var view = CreateTileAt(x, y);
        if (view == null) return;

        var rect = (RectTransform)view.transform;
        rect.anchoredPosition = GridToUIPos(new Vector2Int(x, height));
        tweens.Add(rect.DOAnchorPos(GridToUIPos(new Vector2Int(x, y)), stepDuration).SetEase(Ease.OutCubic));
    }

    private TileType GetRandomTileType()
    {
        return mapData.AllowedTileColors[UnityEngine.Random.Range(0, mapData.AllowedTileColors.Length)];
    }

    private void MoveTile(Vector2Int from, Vector2Int to, List<Tweener> tweens)
    {
        var data = gridData[from.x, from.y];
        var view = gridViews[from.x, from.y];

        // Move to new slot
        gridData[to.x, to.y] = data;
        gridViews[to.x, to.y] = view;

        // IMPORTANT: Clear the old slot properly
        // Instead of a new object, just set the state to Empty so it's ready for reuse
        gridData[from.x, from.y] = new TileData(TileType.Red, from, TilePower.None, TileState.Empty);
        gridViews[from.x, from.y] = null;

        data.GridPosition = to;
        view.Data.GridPosition = to;

        tweens.Add(((RectTransform)view.transform).DOAnchorPos(GridToUIPos(to), stepDuration).SetEase(Ease.OutCubic));
    }

    private bool IsEmpty(int x, int y)
    {
        if (!InBounds(x, y)) return false;
        var d = gridData[x, y];
        return d == null || d.State == TileState.Empty || (d is DestroyableTileData dd && dd.IsDestroyed);
    }

    private bool IsMovable(int x, int y)
    {
        if (!InBounds(x, y)) return false;
        var d = gridData[x, y];
        return d != null && d.State == TileState.Normal;
    }

    private bool InBounds(int x, int y) => x >= 0 && x < width && y >= 0 && y < height;
}