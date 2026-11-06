#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

/// <summary>
/// Creates Localization Settings, en/es locales and UI string tables, and wires static TMP labels.
/// Seeding only adds missing keys and cells; existing English and translated values are never overwritten.
/// </summary>
public static class LocalizationFoundationSetup
{
    private const string RootFolder = "Assets/Localization";
    private const string LocalesFolder = RootFolder + "/Locales";
    private const string TablesFolder = RootFolder + "/Tables";
    private const string SettingsPath = RootFolder + "/LocalizationSettings.asset";

    public const string ExportFolder = RootFolder + "/Export";

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

        SeedMissingEntries(collection, english, spanish);
        WirePrefabLabels();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = settings;
        Debug.Log("[Localization] Foundation ready: en/es, UI table, prefab labels. Existing values were kept. Game View locale menu enabled.");
    }

    public static void WirePrefabLabelsAndSave()
    {
        WirePrefabLabels();
        AssetDatabase.SaveAssets();
        Debug.Log("[Localization] Prefab label wiring complete.");
    }

    public static void ClearSpanishEntries()
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
        AssetDatabase.Refresh();
        Debug.Log("[Localization] Cleared Spanish UI entries. Empty es falls back to en via Spanish Fallback Locale metadata until CSV merge.");
    }

    public static void EnsureSpanishFallsBackToEnglish()
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

    public static void EnsureFolders()
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

    private static void SeedMissingEntries(StringTableCollection collection, Locale english, Locale spanish)
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
            var shared = collection.SharedData.GetEntry(entry.Key) ?? collection.SharedData.AddKey(entry.Key);

            if (enTable.GetEntry(shared.Id) == null)
                enTable.AddEntry(shared.Id, entry.En).IsSmart = entry.Smart;

            if (esTable.GetEntry(shared.Id) == null)
                esTable.AddEntry(shared.Id, string.Empty).IsSmart = entry.Smart;
        }

        EditorUtility.SetDirty(enTable);
        EditorUtility.SetDirty(esTable);
        EditorUtility.SetDirty(collection.SharedData);
        EditorUtility.SetDirty(collection);
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
