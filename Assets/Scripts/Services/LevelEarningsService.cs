using System;
using VContainer;
using VContainer.Unity;

public class LevelEarningsService : ILevelEarningsService, IStartable, IDisposable
{
    private readonly EventScoring eventScoring = new EventScoring();
    private int collectedScore;
    private IGridController gridController;

    [Inject]
    public void Construct(IGridController gridController)
    {
        this.gridController = gridController;
    }

    public void Start()
    {
        gridController.PowerTileCreated += OnPowerTileCreated;
        LevelEvents.OnLevelStarted += OnLevelStarted;
    }

    private void OnLevelStarted(object sender, LevelStartedEventArgs e)
    {
        collectedScore = 0;
    }

    private void OnPowerTileCreated(TileData tilePowerType)
    {
        switch (tilePowerType.Power)
        {
            case TilePower.Bomb:
                collectedScore += eventScoring.pointsForBomb;
                break;
            case TilePower.RowClearer:
            case TilePower.ColumnClearer:
                collectedScore += eventScoring.pointsForLineDestroyer;
                break;
            case TilePower.Rainbow:
                collectedScore += eventScoring.pointsForRainbow;
                break;
        }
    }

    public LevelEarningsResult GetLevelEarnings(int movesRemaining)
    {
        int bonus = Math.Max(0, movesRemaining) * eventScoring.pointsPerUnusedMovement;
        return new LevelEarningsResult(collectedScore, bonus);
    }

    public void Dispose()
    {
        if (gridController != null)
            gridController.PowerTileCreated -= OnPowerTileCreated;

        LevelEvents.OnLevelStarted -= OnLevelStarted;
    }
}

public class EventScoring
{
    public int pointsForLineDestroyer = 1;
    public int pointsForBomb = 2;
    public int pointsForRainbow = 3;
    public int pointsPerUnusedMovement = 4;
    public int pointsPerCombo = 2;
}
