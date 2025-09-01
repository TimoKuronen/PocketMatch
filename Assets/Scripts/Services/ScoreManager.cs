using System;
using System.Collections;
using UnityEngine;

public class ScoreManager : IScoreManager
{
    private EventScoring eventScoring;
    private int collectedScore;

    public void Initialize()
    {
        CoroutineMonoBehavior.Instance.StartCoroutine(SubscribeToEvents());

        eventScoring = new EventScoring();
    }

    private IEnumerator SubscribeToEvents()
    {
        yield return new WaitUntil(() => Services.Get<IGameSessionService>().IsLevelDataLoaded);

        GridController.Instance.PowerTileCreated += OnPowerTileCreated;
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
        collectedScore += Services.Get<ILevelManager>().MovesRemaining * eventScoring.pointsPerUnusedMovement;

        return collectedScore; 
    }

    public void Dispose() 
    {
        GridController.Instance.PowerTileCreated -= OnPowerTileCreated;
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
