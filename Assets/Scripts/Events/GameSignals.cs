using System;
using UnityEngine;

public static class GameSignals
{
    public static event Action OnSessionLoaded;
    public static bool IsSessionLoaded { get; private set; }

    public static int? PendingLevelIndex { get; private set; }

    public static int ActiveLevelIndex { get; private set; } = -1;

    public static void SetPendingLevelIndex(int zeroBasedIndex)
    {
        PendingLevelIndex = zeroBasedIndex;
    }

    public static int? ConsumePendingLevelIndex()
    {
        var index = PendingLevelIndex;
        PendingLevelIndex = null;
        return index;
    }

    public static void SetActiveLevelIndex(int zeroBasedIndex)
    {
        ActiveLevelIndex = zeroBasedIndex;
    }

    public static int ResolveLevelIndex(int savedNextLevelIndex)
    {
        return PendingLevelIndex ?? savedNextLevelIndex;
    }

    public static void MarkSessionLoaded()
    {
        IsSessionLoaded = true;
        OnSessionLoaded?.Invoke();
        Debug.Log("GameSignals: Session marked as loaded.");
    }

    public static void ResetSessionLoaded()
    {
        IsSessionLoaded = false;
        Debug.Log("GameSignals: Session loaded state reset.");
    }
}
