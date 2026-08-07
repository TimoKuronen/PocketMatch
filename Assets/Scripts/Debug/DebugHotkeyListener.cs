using UnityEngine;
using UnityEngine.InputSystem;

public sealed class DebugHotkeyListener : MonoBehaviour
{
    private IDebugToolsService debugTools;

    public void Initialize(IDebugToolsService service)
    {
        debugTools = service;
    }

    private void Update()
    {
        if (!DebugTools.IsEnabled || debugTools == null)
            return;

        if (Keyboard.current == null)
            return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Keyboard.current.wKey.wasPressedThisFrame)
            debugTools.TryExecute(DebugActionIds.ForceWin);

        if (Keyboard.current.lKey.wasPressedThisFrame)
            debugTools.TryExecute(DebugActionIds.ForceLose);
#endif
    }
}
