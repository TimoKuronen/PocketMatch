public static class TilePowerFactory
{
    public static ITilePowerBehavior Get(TilePower power)
    {
        return power switch
        {
            TilePower.RowClearer => new LineClearHorizontal(),
            TilePower.ColumnClearer => new LineClearVertical(),
            TilePower.Bomb => new BombTile(),
            TilePower.Rainbow => new RainbowTile(),
            _ => null
        };
    }
}
