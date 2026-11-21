/// <summary>
/// Objective bookkeeping for a level: which destroyed tiles count toward the goals, and when all goals are met.
/// </summary>
public static class VictoryRules
{
    /// <summary>
    /// Counts one cleared tile. A destroyable counts only toward the destroyable goal; any other tile counts
    /// toward the color goal of its type, which never drops below zero.
    /// </summary>
    public static void RegisterTileDestroyed(VictoryConditions conditions, TileData tile)
    {
        if (tile.State == TileState.Destroyable)
        {
            conditions.DestroyableTileCount--;
            return;
        }

        if (conditions.RequiredColorMatchCount == null)
            return;

        foreach (var match in conditions.RequiredColorMatchCount)
        {
            if (tile.Type != match.TileColor)
                continue;

            match.TileCount--;
            if (match.TileCount < 0)
                match.TileCount = 0;
        }
    }

    public static bool AreMet(VictoryConditions conditions)
    {
        if (conditions.RequiredColorMatchCount != null)
        {
            foreach (var match in conditions.RequiredColorMatchCount)
            {
                if (match.TileCount > 0)
                    return false;
            }
        }

        return conditions.DestroyableTileCount <= 0;
    }
}
