using NUnit.Framework;
using System;
using UnityEngine;

public class ShuffleMatchCountTest
{
    [Test]
    public void ShuffleBoard_ShouldResultInZeroMatches()
    {
        // Arrange
        int width = 6;
        int height = 6;
        TileData[,] grid = CreateTestGrid(width, height);
        MatchFinder matchFinder = new MatchFinder(width, height);
        System.Random rng = new System.Random(12345); // Fixed seed for determinism

        // Act - Shuffle until playable (no matches + has potential moves)
        bool success = GridHelperMethods.ShuffleTypesUntilPlayable(
            grid, width, height, matchFinder, rng, maxAttempts: 200);

        // Assert
        Assert.IsTrue(success, "Shuffle should succeed within max attempts");
        
        var matches = matchFinder.GetMatchGroups(grid);
        Assert.AreEqual(0, matches.Count, "After shuffling, there should be no matches on the board");
        
        bool hasPotentialMoves = GridHelperMethods.HasPotentialMoves(grid, width, height);
        Assert.IsTrue(hasPotentialMoves, "After shuffling, there should be at least one potential move");
    }

    [Test]
    public void ShuffleBoard_MultipleSeeds_ShouldAllResultInZeroMatches()
    {
        // Test with different random seeds to ensure robustness
        for (int seed = 0; seed < 5; seed++)
        {
            int width = 5;
            int height = 5;
            TileData[,] grid = CreateTestGrid(width, height);
            MatchFinder matchFinder = new MatchFinder(width, height);
            System.Random rng = new System.Random(seed);

            bool success = GridHelperMethods.ShuffleTypesUntilPlayable(
                grid, width, height, matchFinder, rng, maxAttempts: 200);

            Assert.IsTrue(success, $"Shuffle should succeed with seed {seed}");
            
            var matches = matchFinder.GetMatchGroups(grid);
            Assert.AreEqual(0, matches.Count, $"With seed {seed}, there should be no matches after shuffle");
            
            bool hasPotentialMoves = GridHelperMethods.HasPotentialMoves(grid, width, height);
            Assert.IsTrue(hasPotentialMoves, $"With seed {seed}, there should be potential moves after shuffle");
        }
    }

    private TileData[,] CreateTestGrid(int width, int height)
    {
        TileData[,] grid = new TileData[width, height];
        
        // Create a simple grid with all normal tiles
        // Types will be randomized by the shuffle function
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid[x, y] = new TileData(
                    TileType.Red, // Initial type, will be shuffled
                    new Vector2Int(x, y),
                    TilePower.None,
                    TileState.Normal
                );
            }
        }
        
        return grid;
    }
}
