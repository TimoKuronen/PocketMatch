using System;

public interface ILevelManager : IUpdateableService
{
    int MovesRemaining { get; }
    Action<LevelManager> OnVictoryConditionsUpdated { get; set; }
    MapData LocalMapData { get; }
    Action OnLevelWon { get; set; }
    Action OnLevelLost { get; set; }
}
