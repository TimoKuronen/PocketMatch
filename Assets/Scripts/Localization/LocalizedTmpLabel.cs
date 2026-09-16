using TMPro;
using UnityEngine;

/// <summary>
/// Binds a TMP label to a UI string table key and refreshes when the selected locale changes.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(TextMeshProUGUI))]
public sealed class LocalizedTmpLabel : MonoBehaviour
{
    [SerializeField] private string entryKey;
    [SerializeField] private TextMeshProUGUI target;

    private ILocalizationService localization;

    private void Awake()
    {
        if (target == null)
            target = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        TryBind();
    }

    private void Start()
    {
        if (localization == null)
            TryBind();
    }

    private void OnDisable()
    {
        if (localization == null)
            return;

        localization.LocaleChanged -= OnLocaleChanged;
        localization = null;
    }

    private void TryBind()
    {
        if (localization != null)
            return;

        if (!LocalizationServiceAccess.TryGet(out localization))
            return;

        localization.LocaleChanged += OnLocaleChanged;
        ApplyText();
    }

    public void SetKey(string key)
    {
        entryKey = key;
        ApplyText();
    }

    private void OnLocaleChanged()
    {
        ApplyText();
    }

    private void ApplyText()
    {
        if (target == null || string.IsNullOrEmpty(entryKey) || localization == null || !localization.IsReady)
            return;

        target.text = localization.Get(entryKey);
    }
}
