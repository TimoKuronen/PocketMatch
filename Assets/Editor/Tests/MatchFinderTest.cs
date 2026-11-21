using System.Linq;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Edit Mode coverage for <see cref="MatchFinder"/>: run detection, blocked cells, group merging, and shape classification.
/// Boards are written as rows from y = 0: R, B, G, Y are normal tiles, # is blocked, . is empty.
/// </summary>
public class MatchFinderTest
{
    [Test]
    public void ThreeInARow_IsOneGroupOfThree()
    {
        var groups = Find("RRRB");

        Assert.AreEqual(1, groups.Count);
        Assert.AreEqual(3, groups[0].Count);
    }

    [Test]
    public void ThreeInAColumn_IsOneGroupOfThree()
    {
        var groups = Find("R", "R", "R", "B");

        Assert.AreEqual(1, groups.Count);
        Assert.AreEqual(3, groups[0].Count);
    }

    [Test]
    public void TwoInARow_IsNoMatch()
    {
        Assert.AreEqual(0, Find("RRBB").Count);
    }

    [Test]
    public void FourInARow_IsOneGroupOfFour()
    {
        var groups = Find("RRRRB");

        Assert.AreEqual(1, groups.Count);
        Assert.AreEqual(4, groups[0].Count);
    }

    [Test]
    public void BlockedOrEmptyCell_BreaksTheRun()
    {
        Assert.AreEqual(0, Find("RR#RR").Count);
        Assert.AreEqual(0, Find("RR.RR").Count);
    }

    [Test]
    public void DifferentColorRuns_AreSeparateGroups()
    {
        var groups = Find("RRRBBB");

        Assert.AreEqual(2, groups.Count);
        CollectionAssert.AreEquivalent(new[] { 3, 3 }, groups.Select(g => g.Count));
    }

    [Test]
    public void SameColorRunsWithAGapBetween_AreSeparateGroups()
    {
        var groups = Find("RRRBRRR");

        Assert.AreEqual(2, groups.Count);
    }

    [Test]
    public void CrossingRuns_MergeIntoOneGroupWithoutDuplicateCells()
    {
        var groups = Find(
            "RRR",
            "RBG",
            "RGB");

        Assert.AreEqual(1, groups.Count);
        Assert.AreEqual(5, groups[0].Count);
        Assert.AreEqual(5, groups[0].Distinct().Count());
    }

    [Test]
    public void AdjacentParallelRunsOfOneColor_MergeIntoOneGroup()
    {
        var groups = Find(
            "RRR",
            "RRR");

        Assert.AreEqual(1, groups.Count);
        Assert.AreEqual(6, groups[0].Count);
    }

    [Test]
    public void DetermineMatchShape_ClassifiesThreeFiveAndBentGroups()
    {
        var finder = new MatchFinder(5, 3);
        var grid = Board("RRRRR", "BGBGB", "GBGBG");

        var line = Positions((0, 0), (1, 0), (2, 0));
        var five = Positions((0, 0), (1, 0), (2, 0), (3, 0), (4, 0));
        var bent = Positions((0, 0), (1, 0), (2, 0), (0, 1), (0, 2));

        Assert.AreEqual(MatchShape.ThreeLine, finder.DetermineMatchShape(line, grid));
        Assert.AreEqual(MatchShape.FiveLine, finder.DetermineMatchShape(five, grid));
        Assert.AreEqual(MatchShape.TOrL, finder.DetermineMatchShape(bent, grid));
    }

    private static System.Collections.Generic.List<System.Collections.Generic.List<Vector2Int>> Find(params string[] rows)
    {
        var grid = Board(rows);
        return new MatchFinder(grid.GetLength(0), grid.GetLength(1)).GetMatchGroups(grid);
    }

    private static TileData[,] Board(params string[] rows)
    {
        int width = rows[0].Length;
        int height = rows.Length;
        var grid = new TileData[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var pos = new Vector2Int(x, y);
                grid[x, y] = rows[y][x] switch
                {
                    '#' => new TileData(TileType.Red, pos, TilePower.None, TileState.Blocked),
                    '.' => new TileData(TileType.Red, pos, TilePower.None, TileState.Empty),
                    'B' => new TileData(TileType.Blue, pos),
                    'G' => new TileData(TileType.Green, pos),
                    'Y' => new TileData(TileType.Yellow, pos),
                    _ => new TileData(TileType.Red, pos)
                };
            }
        }

        return grid;
    }

    private static System.Collections.Generic.List<Vector2Int> Positions(params (int x, int y)[] cells)
    {
        return cells.Select(c => new Vector2Int(c.x, c.y)).ToList();
    }
}
