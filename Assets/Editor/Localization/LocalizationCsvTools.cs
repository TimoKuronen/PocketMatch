#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.Localization.Plugins.CSV;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

/// <summary>
/// String table CSV interchange. One file per table collection, named after the collection.
/// </summary>
public static class LocalizationCsvTools
{
    public static void ExportAll()
    {
        LocalizationFoundationSetup.EnsureFolders();
        var collections = LocalizationEditorSettings.GetStringTableCollections();
        if (collections == null || collections.Count == 0)
        {
            Debug.LogError("[Localization] No string table collections found.");
            return;
        }

        foreach (var collection in collections)
        {
            var assetPath = GetExportCsvAssetPath(collection.TableCollectionName);
            WriteCollectionCsv(collection, assetPath);
            Debug.Log($"[Localization] Wrote {assetPath}");
        }

        AssetDatabase.Refresh();
    }

    public static void ImportWithPicker()
    {
        LocalizationFoundationSetup.EnsureFolders();
        var startDir = Path.GetFullPath(LocalizationFoundationSetup.ExportFolder);
        var absolute = EditorUtility.OpenFilePanel("Import String Table CSV (merge)", startDir, "csv");
        if (string.IsNullOrEmpty(absolute))
            return;

        var collection = ResolveCollectionFromCsvPath(absolute);
        if (collection == null)
            return;

        ImportCsvMerge(collection, absolute);
    }

    private static string GetExportCsvAssetPath(string tableCollectionName)
    {
        return $"{LocalizationFoundationSetup.ExportFolder}/{tableCollectionName}.csv";
    }

    private static StringTableCollection ResolveCollectionFromCsvPath(string absolutePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(absolutePath);
        if (string.IsNullOrEmpty(fileName))
        {
            Debug.LogError("[Localization] Invalid CSV path.");
            return null;
        }

        var collections = LocalizationEditorSettings.GetStringTableCollections();
        foreach (var collection in collections)
        {
            if (string.Equals(collection.TableCollectionName, fileName, System.StringComparison.OrdinalIgnoreCase))
                return collection;
        }

        var names = new StringBuilder();
        foreach (var collection in collections)
        {
            if (names.Length > 0)
                names.Append(", ");
            names.Append(collection.TableCollectionName);
        }

        Debug.LogError(
            $"[Localization] No string table collection named '{fileName}'. " +
            $"Name the CSV after the collection (e.g. UI.csv, Dialogue.csv). Available: {names}");
        return null;
    }

    private static void WriteCollectionCsv(StringTableCollection collection, string assetPath)
    {
        var locales = LocalizationEditorSettings.GetLocales();
        var enLocale = LocalizationEditorSettings.GetLocale("en");
        var targetLocales = new List<Locale>();
        foreach (var locale in locales)
        {
            if (enLocale != null && locale.Identifier == enLocale.Identifier)
                continue;
            targetLocales.Add(locale);
        }

        var sb = new StringBuilder();
        sb.Append("Key,Id");
        if (enLocale != null)
            sb.Append(",English(en)");
        foreach (var locale in targetLocales)
            sb.Append(',').Append(EscapeCsv(locale.LocaleName + "(" + locale.Identifier.Code + ")"));
        sb.AppendLine();

        foreach (var sharedEntry in collection.SharedData.Entries)
        {
            sb.Append(EscapeCsv(sharedEntry.Key)).Append(',').Append(sharedEntry.Id);

            if (enLocale != null)
            {
                var enTable = collection.GetTable(enLocale.Identifier) as StringTable;
                var enValue = enTable?.GetEntry(sharedEntry.Id)?.Value ?? string.Empty;
                sb.Append(',').Append(EscapeCsv(enValue));
            }

            foreach (var locale in targetLocales)
            {
                var table = collection.GetTable(locale.Identifier) as StringTable;
                var value = table?.GetEntry(sharedEntry.Id)?.Value ?? string.Empty;
                sb.Append(',').Append(EscapeCsv(value));
            }

            sb.AppendLine();
        }

        WriteUtf8(assetPath, sb.ToString());
    }

    private static void WriteUtf8(string assetPath, string content)
    {
        var absolute = Path.GetFullPath(assetPath);
        Directory.CreateDirectory(Path.GetDirectoryName(absolute));
        File.WriteAllText(absolute, content, new UTF8Encoding(false));
    }

    // Merge: update locale cells from the CSV; keep keys that are missing from the file.
    private static void ImportCsvMerge(StringTableCollection collection, string absolutePath)
    {
        using (var reader = new StreamReader(absolutePath, Encoding.UTF8))
            Csv.ImportInto(reader, collection, createUndo: true, removeMissingEntries: false);

        foreach (var table in collection.StringTables)
            EditorUtility.SetDirty(table);
        EditorUtility.SetDirty(collection.SharedData);
        EditorUtility.SetDirty(collection);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Localization] Merged CSV into '{collection.TableCollectionName}': {absolutePath}");
    }

    private static string EscapeCsv(string value)
    {
        if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0)
            return value;

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
#endif
