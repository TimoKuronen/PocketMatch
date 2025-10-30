using System;

public static class GameSignals
{
    public static event Action OnSessionLoaded;
    public static bool IsSessionLoaded { get; private set; }

    public static void MarkSessionLoaded()
    {
        IsSessionLoaded = true;
        OnSessionLoaded?.Invoke();
    }
}
