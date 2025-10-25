using UnityEngine;

public static class LevelBuilder
{
    public static TileData[,] BuildLevelFromMapData(MapData mapData)
    {
        int width = mapData.width;
        int height = mapData.height;

        TileData[,] grid = new TileData[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var tile = mapData.GetTile(x, y);

                if (tile.isBlocked)
                {
                    grid[x, y] = new TileData(TileType.Red, new Vector2Int(x, y));
                    grid[x, y].State = TileState.Blocked;
                    continue;
                }
                else if (tile.isDestroyable)
                {
                    grid[x, y] = new DestroyableTileData(new Vector2Int(x, y), 2, false);
                    grid[x, y].State = TileState.Destroyable;
                    continue;
                }

                var data = new TileData(TileType.Red, new Vector2Int(x, y));
                data.State = TileState.Normal;
                data.Power = tile.tilePower;
                grid[x, y] = data;
            }
        }

        return grid;
    }

    public static void SpawnGridViews(
        TileData[,] grid,
        TileView[,] gridViews,
        TilePoolManager tilePoolManager,
        Transform parent)
    {
        int width = grid.GetLength(0);
        int height = grid.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var data = grid[x, y];
                if (data == null)
                    continue;

                var view = tilePoolManager.GetForState(data.State);
                view.transform.SetParent(parent, false);

                RectTransform rect = view.GetComponent<RectTransform>();
                rect.localScale = Vector3.one;
                rect.anchoredPosition = GridController.Instance.GridToUIPos(new Vector2Int(x, y));

                view.Init(data);
                view.gameObject.name = $"Tile_{x}_{y}";
                gridViews[x, y] = view;
            }
        }
    }

    public static void SpawnGridFrames(int width, int height, RectTransform framePrefab, Transform parent)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                RectTransform rect = GameObject.Instantiate(framePrefab, Vector3.zero, Quaternion.identity, parent);
                rect.localScale = Vector3.one;
                rect.anchoredPosition = GridController.Instance.GridToUIPos(new Vector2Int(x, y));
            }
        }
    }
}