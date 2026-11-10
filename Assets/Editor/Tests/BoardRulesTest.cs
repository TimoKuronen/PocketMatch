using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Edit Mode coverage for swap validity, match detection, gravity collapse, and power-shape rules.
/// </summary>
public class BoardRulesTest
{
    [Test]
    public void Swap_ThatDoesNotCreateMatch_LeavesNoMatchGroups()
    {
        int width = 4;
        int height = 4;
        var grid = CreateFilledGrid(width, height, TileType.Red, TileType.Blue);
        // No three-in-a-row possible from swapping (0,0) Red with (1,0) Blue on this layout.
        grid[0, 0] = new TileData(TileType.Red, new Vector2Int(0, 0), TilePower.None, TileState.Normal);
        grid[1, 0] = new TileData(TileType.Blue, new Vector2Int(1, 0), TilePower.None, TileState.Normal);
        grid[2, 0] = new TileData(TileType.Green, new Vector2Int(2, 0), TilePower.None, TileState.Normal);
        grid[3, 0] = new TileData(TileType.Yellow, new Vector2Int(3, 0), TilePower.None, TileState.Normal);

        var matchFinder = new MatchFinder(width, height);
        var controller = new StubGridController(grid, matchFinder);

        Assert.AreEqual(0, matchFinder.GetMatchGroups(grid).Count, "Setup must start without matches");

        var tileA = grid[0, 0];
        var tileB = grid[1, 0];
        controller.SwapTilesInData(new Vector2Int(0, 0), new Vector2Int(1, 0), tileA, tileB);

        Assert.AreEqual(0, matchFinder.GetMatchGroups(grid).Count, "Invalid swap should not create a match");
    }

    [Test]
    public void Swap_ThatCreatesHorizontalMatch_IsDetected()
    {
        int width = 5;
        int height = 3;
        var grid = CreateFilledGrid(width, height, TileType.Blue, TileType.Green);

        // Row y=0: Blue, Blue, Red, Blue, Green — swap Red with right Blue makes three Blues.
        grid[0, 0] = new TileData(TileType.Blue, new Vector2Int(0, 0), TilePower.None, TileState.Normal);
        grid[1, 0] = new TileData(TileType.Blue, new Vector2Int(1, 0), TilePower.None, TileState.Normal);
        grid[2, 0] = new TileData(TileType.Red, new Vector2Int(2, 0), TilePower.None, TileState.Normal);
        grid[3, 0] = new TileData(TileType.Blue, new Vector2Int(3, 0), TilePower.None, TileState.Normal);
        grid[4, 0] = new TileData(TileType.Green, new Vector2Int(4, 0), TilePower.None, TileState.Normal);

        var matchFinder = new MatchFinder(width, height);
        var controller = new StubGridController(grid, matchFinder);

        Assert.AreEqual(0, matchFinder.GetMatchGroups(grid).Count, "Setup must start without matches");

        var tileA = grid[2, 0];
        var tileB = grid[3, 0];
        controller.SwapTilesInData(new Vector2Int(2, 0), new Vector2Int(3, 0), tileA, tileB);

        var matches = matchFinder.GetMatchGroups(grid);
        Assert.GreaterOrEqual(matches.Count, 1, "Valid swap should create at least one match group");
        Assert.GreaterOrEqual(matches[0].Count, 3);
    }

    [Test]
    public void Gravity_CollapsesMovableTilesIntoEmptyCellsBelow()
    {
        int width = 3;
        int height = 4;
        var grid = new TileData[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
                grid[x, y] = GridHelperMethods.CreateEmptyTile(new Vector2Int(x, y));
        }

        // Column 1: empty at y=0, tile at y=2 should fall to y=0 after collapse.
        grid[1, 2] = new TileData(TileType.Red, new Vector2Int(1, 2), TilePower.None, TileState.Normal);

        CollapseAll(grid, width, height);

        Assert.AreEqual(TileType.Red, grid[1, 0].Type);
        Assert.AreEqual(TileState.Normal, grid[1, 0].State);
        Assert.AreEqual(TileState.Empty, grid[1, 2].State);
        Assert.AreEqual(new Vector2Int(1, 0), grid[1, 0].GridPosition);
    }

    [Test]
    public void DetermineMatchShape_FourInAColumn_MapsToFourHorizontalPowerShape()
    {
        // MatchFinder uses same-X (vertical line) as FourHorizontal; CreatePowerTile maps that to RowClearer.
        var positions = new List<Vector2Int>
        {
            new Vector2Int(2, 0),
            new Vector2Int(2, 1),
            new Vector2Int(2, 2),
            new Vector2Int(2, 3)
        };

        int width = 5;
        int height = 5;
        var grid = CreateFilledGrid(width, height, TileType.Red, TileType.Blue);
        foreach (var pos in positions)
            grid[pos.x, pos.y] = new TileData(TileType.Yellow, pos, TilePower.None, TileState.Normal);

        var matchFinder = new MatchFinder(width, height);
        MatchShape shape = matchFinder.DetermineMatchShape(positions, grid);

        Assert.AreEqual(MatchShape.FourHorizontal, shape);
        Assert.AreEqual(TilePower.RowClearer, CreatePowerTileCommand.PowerForShape(shape));
    }

    [Test]
    public void DetermineMatchShape_ThreeLine_HasNoPowerTile()
    {
        var positions = new List<Vector2Int>
        {
            new Vector2Int(0, 1),
            new Vector2Int(1, 1),
            new Vector2Int(2, 1)
        };

        var matchFinder = new MatchFinder(5, 5);
        MatchShape shape = matchFinder.DetermineMatchShape(positions, null);

        Assert.AreEqual(MatchShape.ThreeLine, shape);
        Assert.AreEqual(TilePower.None, CreatePowerTileCommand.PowerForShape(shape));
    }

    [Test]
    public void IsCleared_OnlyNormalAndDestroyedDestroyableTiles()
    {
        var pos = new Vector2Int(0, 0);
        var aliveDestroyable = new DestroyableTileData(pos, 2, false);
        var deadDestroyable = new DestroyableTileData(pos, 1, false);
        deadDestroyable.TakeDamage(1);

        Assert.IsTrue(GridHelperMethods.IsCleared(new TileData(TileType.Blue, pos, TilePower.None, TileState.Normal)));
        Assert.IsTrue(GridHelperMethods.IsCleared(deadDestroyable));
        Assert.IsFalse(GridHelperMethods.IsCleared(aliveDestroyable), "A damaged but alive destroyable stays on the board");
        Assert.IsFalse(GridHelperMethods.IsCleared(new TileData(TileType.Red, pos, TilePower.None, TileState.Blocked)));
        Assert.IsFalse(GridHelperMethods.IsCleared(GridHelperMethods.CreateEmptyTile(pos)));
        Assert.IsFalse(GridHelperMethods.IsCleared(null));
    }

    [Test]
    public void RainbowMostCommonType_IgnoresEmptyAndBlockedCells()
    {
        // Empty and blocked cells are typed Red; only normal tiles may decide the target colour.
        int width = 3;
        int height = 3;
        var grid = new TileData[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
                grid[x, y] = GridHelperMethods.CreateEmptyTile(new Vector2Int(x, y));
        }

        grid[0, 0] = new TileData(TileType.Red, new Vector2Int(0, 0), TilePower.None, TileState.Blocked);
        grid[1, 0] = new TileData(TileType.Blue, new Vector2Int(1, 0), TilePower.None, TileState.Normal);

        Assert.AreEqual(TileType.Blue, RainbowTile.GetMostCommonType(grid, width, height));
    }

    private static void CollapseAll(TileData[,] grid, int width, int height)
    {
        bool changed;
        do
        {
            changed = false;
            for (int y = 1; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!GridHelperMethods.IsMovable(grid, x, y, width, height))
                        continue;
                    if (!GridHelperMethods.IsCellEmpty(grid, x, y - 1, width, height))
                        continue;

                    var from = new Vector2Int(x, y);
                    var to = new Vector2Int(x, y - 1);
                    var data = grid[x, y];
                    grid[to.x, to.y] = data;
                    data.GridPosition = to;
                    grid[from.x, from.y] = GridHelperMethods.CreateEmptyTile(from);
                    changed = true;
                }
            }
        } while (changed);
    }

    private static TileData[,] CreateFilledGrid(int width, int height, TileType a, TileType b)
    {
        var grid = new TileData[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                TileType type = ((x + y) % 2 == 0) ? a : b;
                grid[x, y] = new TileData(type, new Vector2Int(x, y), TilePower.None, TileState.Normal);
            }
        }

        return grid;
    }
}
