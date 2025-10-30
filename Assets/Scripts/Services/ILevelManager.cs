using System;
using VContainer.Unity;

public interface ILevelManager : ITickable
{
    int MovesRemaining { get; }
    Action<LevelManager> OnVictoryConditionsUpdated { get; set; }
    MapData LocalMapData { get; }
    Action OnLevelWon { get; set; }
    Action OnLevelLost { get; set; }
}
