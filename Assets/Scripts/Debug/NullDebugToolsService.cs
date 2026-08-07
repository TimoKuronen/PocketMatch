using System;
using System.Collections.Generic;

public sealed class NullDebugToolsService : IDebugToolsService
{
    public IReadOnlyList<IDebugAction> Actions { get; } = Array.Empty<IDebugAction>();
    public bool HasLevelTarget => false;

    public void RegisterLevelTarget(IDebugLevelTarget target) { }
    public void UnregisterLevelTarget(IDebugLevelTarget target) { }
    public bool TryExecute(string actionId, int intValue = 0) => false;
    public void ForceWin() { }
    public void ForceLose() { }
    public void RefreshPanel() { }
}
