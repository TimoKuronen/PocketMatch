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
                    grid[x, y] = new DestroyableTileData(new Vector2Int(x, y), 3, false);
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

    public static void SpawnGridViews(TileData[,] grid, TileView[,] gridViews, TilePoolManager tilePoolManager, Transform parent, Sprite sprite, TileView prefab, float tileSize, Vector3 offset)
    {
        int width = grid.GetLength(0);
        int height = grid.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] == null)
                    continue;

                var view = tilePoolManager.Get(grid[x, y].State);
                Debug.Log($"Spawning tile with state {grid[x, y].State}");
                view.transform.SetParent(parent, false);
                view.transform.position = new Vector3(x * tileSize, y * tileSize, 0) + offset;
                view.Init(grid[x, y], sprite);
                view.gameObject.name = $"Tile_{x}_{y}";
                gridViews[x, y] = view;
            }
        }
    }
}