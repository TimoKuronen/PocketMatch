#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-shot polish: TMP Auto Size on high-risk localized labels, without baking Localizer-specific naming.
/// </summary>
public static class LocalizationUiPolish
{
    private static readonly HashSet<string> AutoSizeEntryKeys = new HashSet<string>
    {
        LocalizationKeys.LoseContinueCoins,
        LocalizationKeys.LoseContinueAd,
        LocalizationKeys.WinTitle,
        LocalizationKeys.WinNextLevel,
        LocalizationKeys.WinCoinsEarned,
        LocalizationKeys.LoseTitle,
        LocalizationKeys.CommonConfirmTitle,
        LocalizationKeys.CommonConfirmMainMenu,
        LocalizationKeys.PauseConfirmRetry,
        LocalizationKeys.CommonMenu,
        LocalizationKeys.CommonRetry,
        LocalizationKeys.CommonBack,
        LocalizationKeys.CommonSfxVolume,
        LocalizationKeys.MenuLevelsTitle,
        LocalizationKeys.MenuPlay,
        LocalizationKeys.HudMoves,
        LocalizationKeys.HudPuzzleIndex,
        LocalizationKeys.CommonCoinBalance,
        LocalizationKeys.LoadingMessage,
    };

    private static readonly HashSet<string> AutoSizeObjectNames = new HashSet<string>
    {
        "CoinText",
        "MovesLeftText",
        "ConfirmationText",
        "currentLevelText",
        "LoadingText",
    };

    [MenuItem("PocketMatch/Localization/Apply TMP Auto Size Pass")]
    public static void ApplyAutoSizePass()
    {
        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/UI" });
        var changed = 0;

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var root = PrefabUtility.LoadPrefabContents(path);
            var dirty = false;

            foreach (var binder in root.GetComponentsInChildren<LocalizedTmpLabel>(true))
            {
                var so = new SerializedObject(binder);
                var key = so.FindProperty("entryKey")?.stringValue;
                var target = so.FindProperty("target")?.objectReferenceValue as TextMeshProUGUI;
                if (target == null)
                    target = binder.GetComponent<TextMeshProUGUI>();
                if (target == null || string.IsNullOrEmpty(key) || !AutoSizeEntryKeys.Contains(key))
                    continue;

                if (EnableAutoSize(target))
                    dirty = true;
            }

            foreach (var tmp in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (!AutoSizeObjectNames.Contains(tmp.gameObject.name))
                    continue;
                if (EnableAutoSize(tmp))
                    dirty = true;
            }

            // Confirmation dialog body (often named ConfirmationText or message text without LocalizedTmpLabel).
            foreach (var dialog in root.GetComponentsInChildren<ConfirmationDialog>(true))
            {
                var message = FindSerializedTmp(dialog, "messageText");
                if (message != null && EnableAutoSize(message))
                    dirty = true;
            }

            if (dirty)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                changed++;
            }

            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[Localization] TMP Auto Size pass finished. Prefabs updated: {changed}");
    }

    private static TextMeshProUGUI FindSerializedTmp(Object component, string propertyName)
    {
        var so = new SerializedObject(component);
        var prop = so.FindProperty(propertyName);
        return prop?.objectReferenceValue as TextMeshProUGUI;
    }

    private static bool EnableAutoSize(TextMeshProUGUI tmp)
    {
        if (tmp == null)
            return false;

        var so = new SerializedObject(tmp);
        var enable = so.FindProperty("m_enableAutoSizing");
        var min = so.FindProperty("m_fontSizeMin");
        var max = so.FindProperty("m_fontSizeMax");
        var baseSize = so.FindProperty("m_fontSize");

        if (enable == null || min == null || max == null || baseSize == null)
            return false;

        var already = enable.boolValue
            && Mathf.Approximately(min.floatValue, 24f)
            && max.floatValue >= baseSize.floatValue;

        if (already)
            return false;

        enable.boolValue = true;
        min.floatValue = 24f;
        max.floatValue = Mathf.Max(baseSize.floatValue, 72f);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(tmp);
        return true;
    }
}
#endif
