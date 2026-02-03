using System;
using VContainer;
using VContainer.Unity;

public class ScoreService : IScoreService, IStartable, IDisposable
{
    private EventScoring eventScoring;
    private int collectedScore;
    private int movesRemaining;
    private int initialMoveLimit;
    private IGridController gridController;

    [Inject]
    public void Construct(IGridController gridController)
    {
        this.gridController = gridController;
        eventScoring = new EventScoring();
    }

    public void Start()
    {
        gridController.PowerTileCreated += OnPowerTileCreated;
        gridController.ActionTaken += OnActionTaken;
        LevelEvents.OnLevelStarted += OnLevelStarted;
    }

    private void OnLevelStarted(object sender, LevelStartedEventArgs e)
    {
        // Reset score tracking for new level
        collectedScore = 0;
        initialMoveLimit = e.MoveLimit;
        movesRemaining = e.MoveLimit;
    }

    private void OnActionTaken()
    {
        movesRemaining--;
    }

    private void OnPowerTileCreated(TileData tilePowerType)
    {
        switch (tilePowerType.Power)
        {
            case TilePower.Bomb:
                collectedScore += eventScoring.pointsForBomb;
                break;
            case TilePower.RowClearer:
                collectedScore += eventScoring.pointsForLineDestroyer;
                break;
            case TilePower.ColumnClearer:
                collectedScore += eventScoring.pointsForLineDestroyer;
                break;
            case TilePower.Rainbow:
                collectedScore += eventScoring.pointsForRainbow;
                break;
        }
    }

    public int GetTotalScore()
    {
        // Calculate bonus for unused moves
        int bonusScore = movesRemaining * eventScoring.pointsPerUnusedMovement;
        return collectedScore + bonusScore;
    }

    public void Dispose()
    {
        if (gridController != null)
        {
            gridController.PowerTileCreated -= OnPowerTileCreated;
            gridController.ActionTaken -= OnActionTaken;
        }
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
