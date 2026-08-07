#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class DebugToolsEditorMenu
{
    [MenuItem("PocketMatch/Debug/Select Debug Tools Settings")]
    private static void SelectSettings()
    {
        var settings = DebugToolsSettings.Load();
        if (settings != null)
            Selection.activeObject = settings;
    }
}
#endif
