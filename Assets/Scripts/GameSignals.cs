using System;
using UnityEngine;

public static class GameSignals
{
    public static event Action OnSessionLoaded;
    public static bool IsSessionLoaded { get; private set; }

    public static void MarkSessionLoaded()
    {
        IsSessionLoaded = true;
        OnSessionLoaded?.Invoke();
        Debug.Log("GameSignals: Session marked as loaded.");
    }
}
