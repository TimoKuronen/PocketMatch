using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;

public class RefillCommand : ICommand
{
    private TileData[,] gridData;
    private TileView[,] gridViews;
    private int width, height;
    private Func<int, int, TileView> CreateTileAt;
    private Func<Vector2Int, Vector3> GridToWorldPos;
    private Action TileDrop;

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
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (gridData[x, y] == null)
                {
                    var view = CreateTileAt(x, y);
                    Vector3 spawnPos = GridToWorldPos(new Vector2Int(x, y + 3));
                    Vector3 targetPos = GridToWorldPos(new Vector2Int(x, y));

                    view.transform.position = spawnPos;
                    view.transform.DOMove(targetPos, 0.25f).SetEase(Ease.OutCubic);

                    yield return new WaitForSeconds(0.02f);
                    TileDrop?.Invoke();
                }
            }
        }
        yield return new WaitForSeconds(0.3f);
    }
}
