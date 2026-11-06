#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// Menu entry points for localization tooling. Daily CSV and layout tools stay at the top level.
/// </summary>
public static class LocalizationMenu
{
    [MenuItem("PocketMatch/Localization/Export All String Table CSVs")]
    public static void ExportAllStringTableCsvs()
    {
        LocalizationCsvTools.ExportAll();
    }

    [MenuItem("PocketMatch/Localization/Import String Table CSV...")]
    public static void ImportStringTableCsv()
    {
        LocalizationCsvTools.ImportWithPicker();
    }

    [MenuItem("PocketMatch/Localization/Maintenance/Create Foundation")]
    public static void CreateFoundation()
    {
        LocalizationFoundationSetup.CreateFoundation();
    }

    [MenuItem("PocketMatch/Localization/Maintenance/Wire Prefab Labels")]
    public static void WirePrefabLabels()
    {
        LocalizationFoundationSetup.WirePrefabLabelsAndSave();
    }

    [MenuItem("PocketMatch/Localization/Maintenance/Ensure Spanish Falls Back to English")]
    public static void EnsureSpanishFallsBackToEnglish()
    {
        LocalizationFoundationSetup.EnsureSpanishFallsBackToEnglish();
    }

    [MenuItem("PocketMatch/Debug/Localization/Clear Spanish Entries")]
    public static void ClearSpanishEntries()
    {
        var confirmed = EditorUtility.DisplayDialog(
            "Clear Spanish Entries",
            "This empties every Spanish (es) cell in the UI table. Import the CSV again to restore them.",
            "Clear",
            "Cancel");
        if (!confirmed)
            return;

        LocalizationFoundationSetup.ClearSpanishEntries();
    }
}
#endif
