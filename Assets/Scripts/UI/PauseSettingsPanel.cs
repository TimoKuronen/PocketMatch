using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PauseSettingsPanel : UIMenu, IPauseSettingsView
{
    [SerializeField] private Button retryButton;
    [SerializeField] private Button menuButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private TextMeshProUGUI versionText;

    public event System.Action CloseClicked;
    public event System.Action RetryClicked;
    public event System.Action MenuClicked;
    public event System.Action<float> SfxVolumeChanged;

    protected override void Awake()
    {
        base.Awake();
        menuType = MenuType.PauseMenu;

        closeButton.onClick.AddListener(() => CloseClicked?.Invoke());
        retryButton.onClick.AddListener(() => RetryClicked?.Invoke());
        menuButton.onClick.AddListener(() => MenuClicked?.Invoke());
        sfxSlider.onValueChanged.AddListener(value => SfxVolumeChanged?.Invoke(value));
    }

    public void SetSfxVolume(float value)
    {
        if (sfxSlider != null)
            sfxSlider.SetValueWithoutNotify(Mathf.Clamp01(value));
    }

    public void SetVersion(string version)
    {
        if (versionText != null)
            versionText.text = version;
    }

    protected override void OnDestroy()
    {
        closeButton.onClick.RemoveAllListeners();
        retryButton.onClick.RemoveAllListeners();
        menuButton.onClick.RemoveAllListeners();
        sfxSlider.onValueChanged.RemoveAllListeners();
        base.OnDestroy();
    }
}
