using System;

public interface ILevelManager : IUpdateableService
{
    int MovesRemaining { get; }
    Action<LevelManager> VictoryConditionsUpdated { get; set; }
    MapData LocalMapData { get; }
    Action LevelWon { get; set; }
    Action LevelLost { get; set; }
}
