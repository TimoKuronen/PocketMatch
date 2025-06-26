using System;
using UnityEngine;

public class GridService
{
    private TileData[,] grid;
    private int width;
    private int height;
    private System.Random random = new();

    public GridService(int width, int height)
    {
        this.width = width;
        this.height = height;
        grid = new TileData[width, height];

        GenerateInitialGrid();
    }

    private void GenerateInitialGrid()
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                var type = (TileType)random.Next(Enum.GetValues(typeof(TileType)).Length);
                grid[x, y] = new TileData(type, new Vector2Int(x, y));
            }
    }

    public TileData[,] GetGrid() => grid;
}