using System.Collections.Generic;

public interface IDebugToolsService
{
    IReadOnlyList<IDebugAction> Actions { get; }
    bool HasLevelTarget { get; }

    void RegisterLevelTarget(IDebugLevelTarget target);
    void UnregisterLevelTarget(IDebugLevelTarget target);
    bool TryExecute(string actionId, int intValue = 0);
    void ForceWin();
    void ForceLose();
    void RefreshPanel();
}
