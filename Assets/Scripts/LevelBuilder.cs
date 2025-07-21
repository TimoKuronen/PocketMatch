using UnityEngine;
using UnityEngine.Pool;

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
                var tileEditor = mapData.GetTile(x, y);

                if (tileEditor.isBlocked)
                {
                    grid[x, y] = new TileData(TileType.Red, new Vector2Int(x, y));
                    grid[x, y].State = TileState.Blocked;
                    continue;
                }

                var data = new TileData(TileType.Red, new Vector2Int(x, y));
                data.Power = tileEditor.tilePower;
                grid[x, y] = data;
            }
        }

        return grid;
    }

    public static void SpawnGridViews(TileData[,] grid, TileView[,] gridViews, ObjectPool<TileView> pool, Transform parent, Sprite sprite, TileView prefab, float tileSize, Vector3 offset)
    {
        int width = grid.GetLength(0);
        int height = grid.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] == null)
                    continue;

                var view = pool.Get();
                view.transform.SetParent(parent, false);
                view.transform.position = new Vector3(x * tileSize, y * tileSize, 0) + offset;
                view.Init(grid[x, y], sprite);
                view.gameObject.name = $"Tile_{x}_{y}";
                gridViews[x, y] = view;
            }
        }
    }
}