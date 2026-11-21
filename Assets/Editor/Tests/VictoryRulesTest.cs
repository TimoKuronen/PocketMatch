using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Edit Mode coverage for <see cref="VictoryRules"/>: which cleared tiles count toward the goals and when a level is won.
/// </summary>
public class VictoryRulesTest
{
    [Test]
    public void ClearedColorTile_CountsOnlyTowardItsOwnColorGoal()
    {
        var conditions = Conditions(red: 5, blue: 4);

        VictoryRules.RegisterTileDestroyed(conditions, Tile(TileType.Red));

        Assert.AreEqual(4, conditions.RequiredColorMatchCount[0].TileCount);
        Assert.AreEqual(4, conditions.RequiredColorMatchCount[1].TileCount);
        Assert.AreEqual(0, conditions.DestroyableTileCount);
    }

    [Test]
    public void ColorGoal_NeverDropsBelowZero()
    {
        var conditions = Conditions(red: 1, blue: 0);

        VictoryRules.RegisterTileDestroyed(conditions, Tile(TileType.Red));
        VictoryRules.RegisterTileDestroyed(conditions, Tile(TileType.Red));

        Assert.AreEqual(0, conditions.RequiredColorMatchCount[0].TileCount);
    }

    [Test]
    public void ClearedDestroyable_CountsOnlyTowardTheDestroyableGoal()
    {
        var conditions = Conditions(red: 5, blue: 4);
        conditions.DestroyableTileCount = 3;

        VictoryRules.RegisterTileDestroyed(conditions, Tile(TileType.Red, TileState.Destroyable));

        Assert.AreEqual(2, conditions.DestroyableTileCount);
        Assert.AreEqual(5, conditions.RequiredColorMatchCount[0].TileCount);
    }

    [Test]
    public void LevelWithoutColorGoals_IgnoresColorTiles()
    {
        var conditions = new VictoryConditions { RequiredColorMatchCount = null, DestroyableTileCount = 2 };

        Assert.DoesNotThrow(() => VictoryRules.RegisterTileDestroyed(conditions, Tile(TileType.Green)));
        Assert.AreEqual(2, conditions.DestroyableTileCount);
    }

    [Test]
    public void AreMet_RequiresEveryColorGoalAndEveryDestroyableCleared()
    {
        var conditions = Conditions(red: 0, blue: 0);
        Assert.IsTrue(VictoryRules.AreMet(conditions));

        conditions.RequiredColorMatchCount[1].TileCount = 1;
        Assert.IsFalse(VictoryRules.AreMet(conditions));

        conditions.RequiredColorMatchCount[1].TileCount = 0;
        conditions.DestroyableTileCount = 1;
        Assert.IsFalse(VictoryRules.AreMet(conditions));
    }

    [Test]
    public void AreMet_WithNoGoals_IsTrue()
    {
        Assert.IsTrue(VictoryRules.AreMet(new VictoryConditions()));
    }

    private static VictoryConditions Conditions(int red, int blue)
    {
        return new VictoryConditions
        {
            MoveLimit = 20,
            RequiredColorMatchCount = new[]
            {
                new TileMatch { TileColor = TileType.Red, TileCount = red },
                new TileMatch { TileColor = TileType.Blue, TileCount = blue }
            }
        };
    }

    private static TileData Tile(TileType type, TileState state = TileState.Normal)
    {
        return new TileData(type, Vector2Int.zero, TilePower.None, state);
    }
}
