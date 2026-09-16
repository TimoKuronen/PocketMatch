#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Play-mode shortcut to force a board shuffle for deadlock debugging.
/// </summary>
public class ShuffleBoardShortcut : EditorWindow
{
    [MenuItem("PocketMatch/Debug/Shuffle Board")]
    public static void ShowWindow()
    {
        GetWindow<ShuffleBoardShortcut>("Shuffle Board");
    }

    private void OnGUI()
    {
        GUILayout.Label("Board shortcuts", EditorStyles.boldLabel);

        if (!GUILayout.Button("Shuffle"))
            return;

        var gridController = Object.FindFirstObjectByType<GridController>();
        if (gridController == null)
        {
            Debug.LogWarning("[ShuffleBoard] No GridController found. Enter Play mode in the game scene.");
            return;
        }

        gridController.BoardEvaluator.ShuffleBoard();
    }
}
#endif
