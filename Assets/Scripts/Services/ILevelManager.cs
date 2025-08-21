using System;

public interface ILevelManager : IService
{
    public int MovesRemaining { get; }
    Action<LevelManager> VictoryConditionsUpdated { get; set; }
}
