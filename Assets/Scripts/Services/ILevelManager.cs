using System;

public interface ILevelManager
{
    int MovesRemaining { get; }
    MapData LocalMapData { get; }
    VictoryConditions VictoryConditions { get; }
    Action OnVictoryConditionsUpdated { get; set; }
    Action OnLevelWon { get; set; }
    Action OnLevelLost { get; set; }
}
