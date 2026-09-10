#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
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

    private static readonly (string Key, string En, string Es, bool Smart)[] Entries =
    {
        (LocalizationKeys.WinTitle, "You won!", "¡Ganaste!", false),
        (LocalizationKeys.WinNextLevel, "Next Level", "Siguiente nivel", false),
        (LocalizationKeys.WinCoinsEarned, "+{0} coins earned", "+{0} monedas ganadas", true),
        (LocalizationKeys.LoseTitle, "Out of moves!", "¡Sin movimientos!", false),
        (LocalizationKeys.LoseContinueCoins, "Continue: Buy with coins", "Continuar: Comprar con monedas", false),
        (LocalizationKeys.LoseContinueAd, "Continue: Watch Ad", "Continuar: Ver anuncio", false),
        (LocalizationKeys.PauseConfirmRetry, "Are you sure you want to retry this level?", "¿Seguro que quieres reintentar este nivel?", false),
        (LocalizationKeys.MenuTitle, "Rune Match", "Rune Match", false),
        (LocalizationKeys.MenuPlay, "Play", "Jugar", false),
        (LocalizationKeys.MenuLevelsTitle, "Levels", "Niveles", false),
        (LocalizationKeys.HudPuzzleIndex, "Puzzle #{0}", "Puzzle #{0}", true),
        (LocalizationKeys.HudMoves, "Moves: {0}", "Movimientos: {0}", true),
        (LocalizationKeys.LoadingMessage, "Loading", "Cargando", false),
        (LocalizationKeys.CommonMenu, "Menu", "Menú", false),
        (LocalizationKeys.CommonRetry, "Retry", "Reintentar", false),
        (LocalizationKeys.CommonBack, "Back", "Atrás", false),
        (LocalizationKeys.CommonConfirmMainMenu, "Are you sure you want to return to the main menu?", "¿Seguro que quieres volver al menú principal?", false),
        (LocalizationKeys.CommonConfirmTitle, "Are you sure?", "¿Estás seguro?", false),
        (LocalizationKeys.CommonConfirmYes, "Yep", "Sí", false),
        (LocalizationKeys.CommonConfirmNo, "Nope", "No", false),
        (LocalizationKeys.CommonCoinBalance, "x {0}", "x {0}", true),
        (LocalizationKeys.CommonCopyright, "Copyright @Timo Kuronen", "Copyright @Timo Kuronen", false),
        (LocalizationKeys.CommonVersion, "v{0}", "v{0}", true),
        (LocalizationKeys.CommonVersionBuild, "v{0} ({1})", "v{0} ({1})", true),
        (LocalizationKeys.CommonSfxVolume, "SFX Volume", "Volumen SFX", false),
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

            var esEntry = esTable.GetEntry(shared.Id) ?? esTable.AddEntry(shared.Id, entry.Es);
            esEntry.Value = entry.Es;
            esEntry.IsSmart = entry.Smart;
        }

        EditorUtility.SetDirty(enTable);
        EditorUtility.SetDirty(esTable);
        EditorUtility.SetDirty(collection.SharedData);
        EditorUtility.SetDirty(collection);
    }

    private static void WriteSampleCsv()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Key,Id,English(en),Spanish(es)");
        for (var i = 0; i < Mathf.Min(5, Entries.Length); i++)
        {
            var entry = Entries[i];
            sb.Append(entry.Key).Append(',')
                .Append(i).Append(',')
                .Append(EscapeCsv(entry.En)).Append(',')
                .Append(EscapeCsv(entry.Es))
                .AppendLine();
        }

        var absolute = Path.GetFullPath(SampleCsvPath);
        Directory.CreateDirectory(Path.GetDirectoryName(absolute));
        File.WriteAllText(absolute, sb.ToString(), new UTF8Encoding(false));
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
