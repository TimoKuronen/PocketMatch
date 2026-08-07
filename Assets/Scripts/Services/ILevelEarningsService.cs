public readonly struct LevelEarningsResult
{
    public int Collected { get; }
    public int UnusedMoveBonus { get; }
    public int Total => Collected + UnusedMoveBonus;

    public LevelEarningsResult(int collected, int unusedMoveBonus)
    {
        Collected = collected;
        UnusedMoveBonus = unusedMoveBonus;
    }
}

public interface ILevelEarningsService
{
    LevelEarningsResult GetLevelEarnings(int movesRemaining);
}
