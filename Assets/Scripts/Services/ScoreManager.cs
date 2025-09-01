using System;
using System.Collections;
using UnityEngine;

public class ScoreManager : IScoreManager
{
    [field: SerializeField] public EventScoring EventScoring { get; private set; }

    private int collectedScore;

    public void Initialize()
    {
        CoroutineMonoBehavior.Instance.StartCoroutine(SubscribeToEvents());
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
                collectedScore += EventScoring.pointsForBomb;
                break;
            case TilePower.RowClearer:
                collectedScore += EventScoring.pointsForLineDestroyer;
                break;
            case TilePower.ColumnClearer:
                collectedScore += EventScoring.pointsForLineDestroyer;
                break;
            case TilePower.Rainbow:
                collectedScore += EventScoring.pointsForRainbow;
                break;
        }
    }

    public int GetTotalScore() 
    {
        collectedScore += Services.Get<ILevelManager>().MovesRemaining * EventScoring.pointsPerUnusedMovement;
        return collectedScore; 
    }

    public void Dispose() 
    {
        GridController.Instance.PowerTileCreated -= OnPowerTileCreated;
    }
}

[Serializable]
public class EventScoring
{
    public int pointsForLineDestroyer = 1;
    public int pointsForBomb = 2;
    public int pointsForRainbow = 3;
    public int pointsPerUnusedMovement = 4;
    public int pointsPerCombo = 2;
}
