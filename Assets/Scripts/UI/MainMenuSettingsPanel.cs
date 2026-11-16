using UnityEngine;
using UnityEngine.UI;

public class MainMenuSettingsPanel : UIMenu, IMainMenuSettingsView
{
    [SerializeField] private Button closeButton;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Toggle analyticsToggle;

    public event System.Action CloseClicked;
    public event System.Action<float> SfxVolumeChanged;
    public event System.Action<bool> AnalyticsConsentChanged;

    protected override void Awake()
    {
        base.Awake();
        menuType = MenuType.SettingsMenu;

        closeButton.onClick.AddListener(() => CloseClicked?.Invoke());
        sfxSlider.onValueChanged.AddListener(value => SfxVolumeChanged?.Invoke(value));

        if (analyticsToggle != null)
            analyticsToggle.onValueChanged.AddListener(value => AnalyticsConsentChanged?.Invoke(value));
    }

    public override void Open()
    {
        // Settings can open while level select is up, and panels share one container.
        transform.SetAsLastSibling();
        base.Open();
    }

    public void SetSfxVolume(float value)
    {
        if (sfxSlider != null)
            sfxSlider.SetValueWithoutNotify(Mathf.Clamp01(value));
    }

    public void SetAnalyticsConsent(bool granted)
    {
        if (analyticsToggle != null)
            analyticsToggle.SetIsOnWithoutNotify(granted);
    }

    protected override void OnDestroy()
    {
        closeButton.onClick.RemoveAllListeners();
        sfxSlider.onValueChanged.RemoveAllListeners();

        if (analyticsToggle != null)
            analyticsToggle.onValueChanged.RemoveAllListeners();
        base.OnDestroy();
    }
}
