using System;

public interface ILevelManager
{
    int MovesRemaining { get; }
    bool IsLevelEnded { get; }
    MapData LocalMapData { get; }
    VictoryConditions VictoryConditions { get; }
    event Action OnVictoryConditionsUpdated;
    event Action OnLevelWon;
    event Action OnLevelLost;
    event Action OnLevelContinued;
    void GrantExtraMoves(int count);
}
