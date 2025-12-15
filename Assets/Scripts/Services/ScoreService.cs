using System.Collections;
using UnityEngine;
using VContainer;

public class ScoreService : IScoreService
{
    private EventScoring eventScoring;
    private int collectedScore;
    private IGridController gridController;

    [Inject]
    public void Construct(IGridController gridController)
    {
        this.gridController = gridController;
        CoroutineMonoBehavior.Instance.StartCoroutine(SubscribeToEvents());

        eventScoring = new EventScoring();
    }

    private IEnumerator SubscribeToEvents()
    {
        yield return new WaitUntil(() => GameSignals.IsSessionLoaded);

        gridController.PowerTileCreated += OnPowerTileCreated;
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
        collectedScore += LevelManager.MovesRemaining * eventScoring.pointsPerUnusedMovement;

        return collectedScore;
    }

    public void Dispose()
    {
        if (gridController != null)
        {
            gridController.PowerTileCreated -= OnPowerTileCreated;
        }
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
