using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using System;

public class MainMenuPanel : UIMenu, IMainMenuView
{
    [SerializeField] private TextMeshProUGUI versionText;
    [SerializeField] private TextMeshProUGUI coinCountText;
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private ConfirmationDialog confirmationDialog;

    private MenuStackManager menuStackManager;
    private ILocalizationService localization;

    public event Action PlayClicked;
    public event Action SettingsClicked;

    [Inject]
    public void Construct(MenuStackManager menuStackManager, ILocalizationService localization)
    {
        this.menuStackManager = menuStackManager;
        this.localization = localization;
    }

    protected override void Awake()
    {
        // Main menu stays visible on load; base UIMenu Awake would hide menuPanel.
        if (menuPanel == null)
        {
            menuPanel = gameObject;
        }

        menuType = MenuType.PauseMenu;

        if (menuPanel != null)
        {
            menuPanel.SetActive(true);
        }

        IsOpen = true;

        playButton.onClick.AddListener(() => PlayClicked?.Invoke());
        settingsButton.onClick.AddListener(() => SettingsClicked?.Invoke());
    }

    public void SetCoinCount(int coins)
    {
        if (localization == null)
        {
            coinCountText.text = $"x {coins}";
            return;
        }

        coinCountText.text = localization.Get(LocalizationKeys.CommonCoinBalance, coins);
    }

    public void SetVersion(string version)
    {
        if (versionText != null)
        {
            versionText.text = version;
        }
    }

    protected override void OnDestroy()
    {
        playButton.onClick.RemoveAllListeners();
        settingsButton.onClick.RemoveAllListeners();

        base.OnDestroy();
    }
}
