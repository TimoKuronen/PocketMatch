using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

/// <summary>
/// Binds a TMP label to a UI string table key and refreshes when the selected locale changes.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(TextMeshProUGUI))]
public sealed class LocalizedTmpLabel : MonoBehaviour
{
    [SerializeField] private string entryKey;
    [SerializeField] private TextMeshProUGUI target;

    private int refreshVersion;

    private void Awake()
    {
        if (target == null)
            target = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        Refresh();
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        refreshVersion++;
    }

    public void SetKey(string key)
    {
        entryKey = key;
        Refresh();
    }

    private void OnLocaleChanged(UnityEngine.Localization.Locale _)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (target == null || string.IsNullOrEmpty(entryKey) || !LocalizationSettings.HasSettings)
            return;

        refreshVersion++;
        RefreshAsync(refreshVersion).Forget();
    }

    private async UniTaskVoid RefreshAsync(int version)
    {
        var init = LocalizationSettings.InitializationOperation;
        await UniTask.WaitUntil(() => init.IsDone || version != refreshVersion);
        if (version != refreshVersion || this == null)
            return;

        // Sync GetLocalizedString must not run inside Addressables completion callbacks.
        await UniTask.Yield(PlayerLoopTiming.Update);
        if (version != refreshVersion || this == null || !isActiveAndEnabled)
            return;

        ApplyText();
    }

    private void ApplyText()
    {
        if (target == null || string.IsNullOrEmpty(entryKey) || !LocalizationSettings.HasSettings)
            return;

        target.text = LocalizationSettings.StringDatabase.GetLocalizedString(
            LocalizationKeys.UiTable,
            entryKey);
    }
}
