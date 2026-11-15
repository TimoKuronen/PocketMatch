using NUnit.Framework;
using System;
using UnityEngine;

public class ShuffleMatchCountTest
{
    [TestCase(6, 12345)]
    [TestCase(5, 0)]
    [TestCase(5, 1)]
    [TestCase(5, 4)]
    public void ShuffleTypesUntilPlayable_LeavesNoMatchesAndAtLeastOneMove(int size, int seed)
    {
        TileData[,] grid = CreateTestGrid(size, size);
        MatchFinder matchFinder = new MatchFinder(size, size);
        System.Random rng = new System.Random(seed);

        bool success = GridHelperMethods.ShuffleTypesUntilPlayable(
            grid, size, size, matchFinder, rng, maxAttempts: 200);

        Assert.IsTrue(success, "Shuffle should succeed within max attempts");
        Assert.AreEqual(0, matchFinder.GetMatchGroups(grid).Count, "No matches after shuffle");
        Assert.IsTrue(GridHelperMethods.HasPotentialMoves(grid, size, size), "At least one potential move after shuffle");
    }

    private TileData[,] CreateTestGrid(int width, int height)
    {
        // Shuffle only permutes existing types, so the bag must include multiple colors.
        TileType[] types =
        {
            TileType.Red,
            TileType.Blue,
            TileType.Green,
            TileType.Yellow
        };

        TileData[,] grid = new TileData[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                TileType type = types[(x + y * 3) % types.Length];
                grid[x, y] = new TileData(
                    type,
                    new Vector2Int(x, y),
                    TilePower.None,
                    TileState.Normal);
            }
        }

        return grid;
    }
}
