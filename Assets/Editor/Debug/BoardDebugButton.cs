using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Editor-only HUD button for marking board bugs during play mode.
/// </summary>
public class BoardDebugButton : MonoBehaviour
{
    [Tooltip("Reference to the active GridController in the scene.")]
    public GridController gridController;

    [Tooltip("Optional: also allow F9 hotkey to mark a bug.")]
    public bool enableHotkey = true;

    [Tooltip("Key used to mark a bug when enableHotkey is true.")]
    public KeyCode hotkey = KeyCode.F9;

    public void MarkBug()
    {
        if (!BoardDebugConfig.IsEnabled)
        {
            Debug.LogWarning("[BoardDebugButton] Board debug logging is disabled in settings.");
            return;
        }

        if (gridController == null)
        {
            Debug.LogError("[BoardDebugButton] GridController reference is not set.");
            return;
        }

        var data = gridController.GridDataForValidation;
        int w = gridController.GridWidthForValidation;
        int h = gridController.GridHeightForValidation;

        if (data == null)
        {
            Debug.LogError("[BoardDebugButton] Grid data is null (board may not be initialized).");
            return;
        }

        var logger = BoardDebugLogger.Instance;

        logger.LogBoard(
            "UserMarkedBug",
            data,
            w,
            h,
            new Dictionary<string, string>
            {
                { "note", "User reports a visual/logic issue at this moment." }
            });

        logger.Flush();

        Debug.Log($"[BoardDebug] Bug marked. Log written to: {logger.GetFilePath()}");
    }

    private void Update()
    {
        if (!enableHotkey)
            return;

        if (Keyboard.current.f9Key.wasPressedThisFrame)
        {
            MarkBug();
        }
    }
}
