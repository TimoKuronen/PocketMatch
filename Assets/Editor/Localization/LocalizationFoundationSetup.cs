#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.Localization.Plugins.CSV;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

/// <summary>
/// Creates Localization Settings, en/es locales, UI string tables, sample CSV, and wires static TMP labels.
/// </summary>
public static class LocalizationFoundationSetup
{
    private const string RootFolder = "Assets/Localization";
    private const string LocalesFolder = RootFolder + "/Locales";
    private const string TablesFolder = RootFolder + "/Tables";
    private const string ExportFolder = RootFolder + "/Export";
    private const string SettingsPath = RootFolder + "/LocalizationSettings.asset";
    private const string SampleCsvPath = ExportFolder + "/ui-sample.csv";

    // Target locale cells stay empty until an approved CSV is merged back in.
    private static readonly (string Key, string En, bool Smart)[] Entries =
    {
        (LocalizationKeys.WinTitle, "You won!", false),
        (LocalizationKeys.WinNextLevel, "Next Level", false),
        (LocalizationKeys.WinCoinsEarned, "+{0} coins earned", true),
        (LocalizationKeys.LoseTitle, "Out of moves!", false),
        (LocalizationKeys.LoseContinueCoins, "Continue: Buy with coins", false),
        (LocalizationKeys.LoseContinueAd, "Continue: Watch Ad", false),
        (LocalizationKeys.PauseConfirmRetry, "Are you sure you want to retry this level?", false),
        (LocalizationKeys.MenuTitle, "Rune Match", false),
        (LocalizationKeys.MenuPlay, "Play", false),
        (LocalizationKeys.MenuLevelsTitle, "Levels", false),
        (LocalizationKeys.HudPuzzleIndex, "Puzzle #{0}", true),
        (LocalizationKeys.HudMoves, "Moves: {0}", true),
        (LocalizationKeys.LoadingMessage, "Loading", false),
        (LocalizationKeys.CommonMenu, "Menu", false),
        (LocalizationKeys.CommonRetry, "Retry", false),
        (LocalizationKeys.CommonBack, "Back", false),
        (LocalizationKeys.CommonConfirmMainMenu, "Are you sure you want to return to the main menu?", false),
        (LocalizationKeys.CommonConfirmTitle, "Are you sure?", false),
        (LocalizationKeys.CommonConfirmYes, "Yep", false),
        (LocalizationKeys.CommonConfirmNo, "Nope", false),
        (LocalizationKeys.CommonCoinBalance, "x {0}", true),
        (LocalizationKeys.CommonCloudWaiting, "Cloud: waiting", false),
        (LocalizationKeys.CommonCloudReady, "Cloud: ready", false),
        (LocalizationKeys.CommonCloudOffline, "Cloud: offline (local only)", false),
        (LocalizationKeys.CommonCloudInitFailed, "Cloud: init failed (local only)", false),
        (LocalizationKeys.CommonCloudUploadFailed, "Cloud: upload failed (local OK)", false),
        (LocalizationKeys.CommonCloudApplied, "Cloud: applied over local", false),
        (LocalizationKeys.CommonCloudUnknown, "Cloud: unknown", false),
        (LocalizationKeys.CommonCopyright, "Copyright @Timo Kuronen", false),
        (LocalizationKeys.CommonVersion, "v{0}", true),
        (LocalizationKeys.CommonVersionBuild, "v{0} ({1})", true),
        (LocalizationKeys.CommonSettingsFooter, "{0}\n{1}", true),
        (LocalizationKeys.CommonSfxVolume, "SFX Volume", false),
    };

    private static readonly (string EnglishText, string Key)[] PrefabLabelMap =
    {
        ("You won!", LocalizationKeys.WinTitle),
        ("Next Level", LocalizationKeys.WinNextLevel),
        ("Out of moves!", LocalizationKeys.LoseTitle),
        ("Continue: Buy with coins", LocalizationKeys.LoseContinueCoins),
        ("Continue: Watch Ad", LocalizationKeys.LoseContinueAd),
        ("Menu", LocalizationKeys.CommonMenu),
        ("Retry", LocalizationKeys.CommonRetry),
        ("Back", LocalizationKeys.CommonBack),
        ("SFX Volume", LocalizationKeys.CommonSfxVolume),
        ("Copyright @Timo Kuronen", LocalizationKeys.CommonCopyright),
        ("Rune Match", LocalizationKeys.MenuTitle),
        ("Play", LocalizationKeys.MenuPlay),
        ("Levels", LocalizationKeys.MenuLevelsTitle),
        ("Are you sure?", LocalizationKeys.CommonConfirmTitle),
        ("Yep", LocalizationKeys.CommonConfirmYes),
        ("Nope", LocalizationKeys.CommonConfirmNo),
        ("Loading ...", LocalizationKeys.LoadingMessage),
        ("Loading...", LocalizationKeys.LoadingMessage),
    };

    [MenuItem("PocketMatch/Localization/Create Foundation")]
    public static void CreateFoundation()
    {
        EnsureFolders();

        var settings = GetOrCreateSettings();
        var english = GetOrCreateLocale("en", "English");
        var spanish = GetOrCreateLocale("es", "Spanish");

        LocalizationEditorSettings.AddLocale(english);
        LocalizationEditorSettings.AddLocale(spanish);
        EnsureLocaleFallback(spanish, english);
        LocalizationEditorSettings.ActiveLocalizationSettings = settings;
        LocalizationEditorSettings.ShowLocaleMenuInGameView = true;

        var settingsSo = new SerializedObject(settings);
        var initSync = settingsSo.FindProperty("m_InitializeSynchronously");
        if (initSync != null)
        {
            initSync.boolValue = true;
            settingsSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
        }

        var collection = LocalizationEditorSettings.GetStringTableCollection(LocalizationKeys.UiTable);
        if (collection == null)
        {
            collection = LocalizationEditorSettings.CreateStringTableCollection(
                LocalizationKeys.UiTable,
                TablesFolder,
                new List<Locale> { english, spanish });
        }

        PopulateTables(collection, english, spanish);
        WriteSampleCsv();
        WirePrefabLabels();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = settings;
        Debug.Log("[Localization] Foundation ready: en/es, UI table, sample CSV, prefab labels. Game View locale menu enabled.");
    }

    [MenuItem("PocketMatch/Localization/Wire Prefab Labels")]
    public static void WirePrefabLabelsMenu()
    {
        WirePrefabLabels();
        AssetDatabase.SaveAssets();
        Debug.Log("[Localization] Prefab label wiring complete.");
    }

    [MenuItem("PocketMatch/Localization/Export Sample CSV")]
    public static void ExportSampleCsvMenu()
    {
        EnsureFolders();
        WriteSampleCsv();
        AssetDatabase.Refresh();
        Debug.Log($"[Localization] Wrote {SampleCsvPath}");
    }

    [MenuItem("PocketMatch/Localization/Export All String Table CSVs")]
    public static void ExportAllStringTableCsvsMenu()
    {
        EnsureFolders();
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

    [MenuItem("PocketMatch/Localization/Import String Table CSV...")]
    public static void ImportStringTableCsvMenu()
    {
        EnsureFolders();
        var startDir = Path.GetFullPath(ExportFolder);
        var absolute = EditorUtility.OpenFilePanel("Import String Table CSV (merge)", startDir, "csv");
        if (string.IsNullOrEmpty(absolute))
            return;

        var collection = ResolveCollectionFromCsvPath(absolute);
        if (collection == null)
            return;

        ImportCsvMerge(collection, absolute);
    }

    [MenuItem("PocketMatch/Localization/Clear Spanish Entries")]
    public static void ClearSpanishEntriesMenu()
    {
        var collection = LocalizationEditorSettings.GetStringTableCollection(LocalizationKeys.UiTable);
        if (collection == null)
        {
            Debug.LogError("[Localization] UI string table collection not found.");
            return;
        }

        var esTable = collection.GetTable("es") as StringTable;
        if (esTable == null)
        {
            Debug.LogError("[Localization] Spanish UI table not found.");
            return;
        }

        foreach (var entry in esTable.Values)
            entry.Value = string.Empty;

        var english = LocalizationEditorSettings.GetLocale("en");
        var spanish = LocalizationEditorSettings.GetLocale("es");
        EnsureLocaleFallback(spanish, english);

        EditorUtility.SetDirty(esTable);
        AssetDatabase.SaveAssets();
        WriteSampleCsv();
        AssetDatabase.Refresh();
        Debug.Log("[Localization] Cleared Spanish UI entries. Empty es falls back to en via Spanish Fallback Locale metadata until CSV merge.");
    }

    [MenuItem("PocketMatch/Localization/Ensure Spanish Falls Back to English")]
    public static void EnsureSpanishFallsBackToEnglishMenu()
    {
        var english = LocalizationEditorSettings.GetLocale("en");
        var spanish = LocalizationEditorSettings.GetLocale("es");
        if (english == null || spanish == null)
        {
            Debug.LogError("[Localization] en/es locales not found. Run Create Foundation first.");
            return;
        }

        EnsureLocaleFallback(spanish, english);
        AssetDatabase.SaveAssets();
        Debug.Log("[Localization] Spanish Fallback Locale set to English.");
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder(RootFolder))
            AssetDatabase.CreateFolder("Assets", "Localization");
        if (!AssetDatabase.IsValidFolder(LocalesFolder))
            AssetDatabase.CreateFolder(RootFolder, "Locales");
        if (!AssetDatabase.IsValidFolder(TablesFolder))
            AssetDatabase.CreateFolder(RootFolder, "Tables");
        if (!AssetDatabase.IsValidFolder(ExportFolder))
            AssetDatabase.CreateFolder(RootFolder, "Export");
    }

    private static LocalizationSettings GetOrCreateSettings()
    {
        var existing = LocalizationEditorSettings.ActiveLocalizationSettings;
        if (existing != null)
            return existing;

        existing = AssetDatabase.LoadAssetAtPath<LocalizationSettings>(SettingsPath);
        if (existing != null)
            return existing;

        var settings = ScriptableObject.CreateInstance<LocalizationSettings>();
        settings.name = "Localization Settings";
        AssetDatabase.CreateAsset(settings, SettingsPath);
        return settings;
    }

    private static Locale GetOrCreateLocale(string code, string displayName)
    {
        var existing = LocalizationEditorSettings.GetLocale(code);
        if (existing != null)
            return existing;

        var path = $"{LocalesFolder}/{displayName} ({code}).asset";
        existing = AssetDatabase.LoadAssetAtPath<Locale>(path);
        if (existing != null)
            return existing;

        var locale = Locale.CreateLocale(new LocaleIdentifier(code));
        locale.name = $"{displayName} ({code})";
        AssetDatabase.CreateAsset(locale, path);
        return locale;
    }

    private static void EnsureLocaleFallback(Locale locale, Locale fallback)
    {
        if (locale == null || fallback == null || locale == fallback)
            return;

        var existing = locale.Metadata.GetMetadata<FallbackLocale>();
        if (existing != null)
        {
            if (existing.Locale == fallback)
                return;

            existing.Locale = fallback;
            EditorUtility.SetDirty(locale);
            return;
        }

        locale.Metadata.AddMetadata(new FallbackLocale(fallback));
        EditorUtility.SetDirty(locale);
    }

    private static void PopulateTables(StringTableCollection collection, Locale english, Locale spanish)
    {
        var enTable = collection.GetTable(english.Identifier) as StringTable;
        var esTable = collection.GetTable(spanish.Identifier) as StringTable;
        if (enTable == null || esTable == null)
        {
            Debug.LogError("[Localization] UI string tables missing for en/es.");
            return;
        }

        foreach (var entry in Entries)
        {
            var shared = collection.SharedData.GetEntry(entry.Key);
            if (shared == null)
                shared = collection.SharedData.AddKey(entry.Key);

            var enEntry = enTable.GetEntry(shared.Id) ?? enTable.AddEntry(shared.Id, entry.En);
            enEntry.Value = entry.En;
            enEntry.IsSmart = entry.Smart;

            var esEntry = esTable.GetEntry(shared.Id) ?? esTable.AddEntry(shared.Id, string.Empty);
            esEntry.Value = string.Empty;
            esEntry.IsSmart = entry.Smart;
        }

        EditorUtility.SetDirty(enTable);
        EditorUtility.SetDirty(esTable);
        EditorUtility.SetDirty(collection.SharedData);
        EditorUtility.SetDirty(collection);
    }

    private static void WriteSampleCsv()
    {
        var collection = LocalizationEditorSettings.GetStringTableCollection(LocalizationKeys.UiTable);
        if (collection == null)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Key,Id,English(en),Spanish(es)");
            for (var i = 0; i < Mathf.Min(5, Entries.Length); i++)
            {
                var entry = Entries[i];
                sb.Append(entry.Key).Append(',')
                    .Append(i).Append(',')
                    .Append(EscapeCsv(entry.En)).Append(',')
                    .AppendLine();
            }

            WriteUtf8(SampleCsvPath, sb.ToString());
            return;
        }

        WriteCollectionCsv(collection, SampleCsvPath, maxRows: 5);
    }

    private static string GetExportCsvAssetPath(string tableCollectionName)
    {
        return $"{ExportFolder}/{tableCollectionName}.csv";
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

    private static void WriteCollectionCsv(StringTableCollection collection, string path = null, int maxRows = int.MaxValue)
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

        var rowIndex = 0;
        foreach (var sharedEntry in collection.SharedData.Entries)
        {
            if (rowIndex >= maxRows)
                break;

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
            rowIndex++;
        }

        WriteUtf8(path ?? GetExportCsvAssetPath(collection.TableCollectionName), sb.ToString());
    }

    private static void WriteUtf8(string assetPath, string content)
    {
        var absolute = Path.GetFullPath(assetPath);
        Directory.CreateDirectory(Path.GetDirectoryName(absolute));
        File.WriteAllText(absolute, content, new UTF8Encoding(false));
    }

    private static void ImportCsvMerge(StringTableCollection collection, string absolutePath)
    {
        // Merge: update locale cells from CSV; keep keys that are missing from the file.
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

    private static void WirePrefabLabels()
    {
        var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/UI" });
        foreach (var guid in prefabGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var root = PrefabUtility.LoadPrefabContents(path);
            var dirty = false;

            foreach (var tmp in root.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
            {
                var text = tmp.text != null ? tmp.text.Trim() : string.Empty;
                if (string.IsNullOrEmpty(text))
                    continue;

                string key = null;
                foreach (var map in PrefabLabelMap)
                {
                    if (map.EnglishText == text)
                    {
                        key = map.Key;
                        break;
                    }
                }

                if (key == null)
                    continue;

                var binder = tmp.GetComponent<LocalizedTmpLabel>();
                if (binder == null)
                    binder = tmp.gameObject.AddComponent<LocalizedTmpLabel>();

                var so = new SerializedObject(binder);
                so.FindProperty("entryKey").stringValue = key;
                so.FindProperty("target").objectReferenceValue = tmp;
                so.ApplyModifiedPropertiesWithoutUndo();
                dirty = true;
            }

            if (dirty)
                PrefabUtility.SaveAsPrefabAsset(root, path);

            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
#endif
