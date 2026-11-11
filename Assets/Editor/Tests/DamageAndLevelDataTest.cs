using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Edit Mode coverage for destroyable tile hit points from level data and adjacent damage per match group.
/// </summary>
public class DamageAndLevelDataTest
{
    [Test]
    public void LevelBuilder_ReadsDestroyableHitPointsFromMapData()
    {
        var map = ScriptableObject.CreateInstance<MapData>();
        map.width = 2;
        map.height = 1;
        map.tiles = new[]
        {
            new TileDataEditorView { isDestroyable = true, hitPoints = 3 },
            new TileDataEditorView { isDestroyable = true, hitPoints = 0 }
        };

        var grid = LevelBuilder.BuildLevelFromMapData(map);

        Assert.AreEqual(3, ((DestroyableTileData)grid[0, 0]).HitPoints);
        Assert.AreEqual(1, ((DestroyableTileData)grid[1, 0]).HitPoints, "Hit points below 1 clamp to 1");
        Object.DestroyImmediate(map);
    }

    [Test]
    public void AdjacentDamage_OneMatchGroupDealsOneHitEvenWithManyAdjacentTiles()
    {
        var grid = new TileData[3, 2];
        FillNormal(grid);
        var destroyable = new DestroyableTileData(new Vector2Int(1, 1), 3, false);
        grid[1, 1] = destroyable;

        var group = new List<Vector2Int> { new(0, 0), new(1, 0), new(2, 0) };
        AdjacentDamageProcessor.GetAdjacentDestroyables(new List<List<Vector2Int>> { group }, grid);

        Assert.AreEqual(2, destroyable.HitPoints, "Only the middle tile touches the destroyable, and one group is one hit");
    }

    [Test]
    public void AdjacentDamage_ReportsDestroyableOnlyOnceWhenTwoGroupsTouchIt()
    {
        var grid = new TileData[3, 3];
        FillNormal(grid);
        var destroyable = new DestroyableTileData(new Vector2Int(1, 1), 2, false);
        grid[1, 1] = destroyable;

        var above = new List<Vector2Int> { new(0, 2), new(1, 2), new(2, 2) };
        var below = new List<Vector2Int> { new(0, 0), new(1, 0), new(2, 0) };
        var destroyed = AdjacentDamageProcessor.GetAdjacentDestroyables(new List<List<Vector2Int>> { above, below }, grid);

        Assert.IsTrue(destroyable.IsDestroyed);
        Assert.AreEqual(1, destroyed.Count);
        Assert.AreEqual(new Vector2Int(1, 1), destroyed[0]);
    }

    private static void FillNormal(TileData[,] grid)
    {
        for (int x = 0; x < grid.GetLength(0); x++)
            for (int y = 0; y < grid.GetLength(1); y++)
                grid[x, y] = new TileData(TileType.Blue, new Vector2Int(x, y), TilePower.None, TileState.Normal);
    }
}
