using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuSettingsPanel : UIMenu, IMainMenuSettingsView
{
    [SerializeField] private Button closeButton;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private TextMeshProUGUI versionText;

    public event System.Action CloseClicked;
    public event System.Action<float> SfxVolumeChanged;

    protected override void Awake()
    {
        base.Awake();
        menuType = MenuType.SettingsMenu;

        closeButton.onClick.AddListener(() => CloseClicked?.Invoke());
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
        sfxSlider.onValueChanged.RemoveAllListeners();
        base.OnDestroy();
    }
}
