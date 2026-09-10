#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class DebugToolsEditorMenu
{
    [MenuItem("PocketMatch/Debug/Select Debug Tools Settings")]
    private static void SelectSettings()
    {
        var settings = Resources.Load<DebugToolsSettings>("DebugToolsSettings");
        if (settings == null)
        {
            Debug.LogWarning("[DebugTools] DebugToolsSettings asset not found under Resources.");
            return;
        }

        Selection.activeObject = settings;
        EditorGUIUtility.PingObject(settings);
    }
}
#endif
