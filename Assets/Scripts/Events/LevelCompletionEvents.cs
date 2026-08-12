using System;

/// <summary>
/// Win payload; <see cref="TotalScore"/> becomes coin rewards unless <see cref="IsLevelCapReached"/> is set.
/// </summary>
public class LevelCompletedEventArgs : EventArgs
{
    public string LevelName { get; }
    public int MovesRemaining { get; }
    public int MovesSpent { get; }
    public int TotalScore { get; }
    public int GameTimeInSeconds { get; }
    public bool IsLevelCapReached { get; }
    public int CompletedLevelIndex { get; }

    public LevelCompletedEventArgs(
        string levelName,
        int movesRemaining,
        int movesSpent,
        int totalScore,
        int gameTimeInSeconds,
        bool isLevelCapReached,
        int completedLevelIndex)
    {
        LevelName = levelName;
        MovesRemaining = movesRemaining;
        MovesSpent = movesSpent;
        TotalScore = totalScore;
        GameTimeInSeconds = gameTimeInSeconds;
        IsLevelCapReached = isLevelCapReached;
        CompletedLevelIndex = completedLevelIndex;
    }
}

public class LevelFailedEventArgs : EventArgs
{
    public string LevelName { get; }
    public int GameTimeInSeconds { get; }

    public LevelFailedEventArgs(string levelName, int gameTimeInSeconds)
    {
        LevelName = levelName;
        GameTimeInSeconds = gameTimeInSeconds;
    }
}

public class LevelStartedEventArgs : EventArgs
{
    public string LevelName { get; }
    public int LevelIndex { get; }
    public int MoveLimit { get; }

    public LevelStartedEventArgs(string levelName, int levelIndex, int moveLimit)
    {
        LevelName = levelName;
        LevelIndex = levelIndex;
        MoveLimit = moveLimit;
    }
}

/// <summary>
/// Static level lifecycle events (start, win, fail) for analytics, save, and economy subscribers.
/// </summary>
public static class LevelEvents
{
    public static event EventHandler<LevelCompletedEventArgs> OnLevelCompleted;
    public static event EventHandler<LevelFailedEventArgs> OnLevelFailed;
    public static event EventHandler<LevelStartedEventArgs> OnLevelStarted;

    public static void RaiseLevelCompleted(LevelCompletedEventArgs args)
    {
        OnLevelCompleted?.Invoke(null, args);
    }

    public static void RaiseLevelFailed(LevelFailedEventArgs args)
    {
        OnLevelFailed?.Invoke(null, args);
    }

    public static void RaiseLevelStarted(LevelStartedEventArgs args)
    {
        OnLevelStarted?.Invoke(null, args);
    }
}
