using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using Cysharp.Threading.Tasks;
using System;

/// <summary>
/// Main menu panel displayed in the main menu scene.
/// Acts as a View in the MVP pattern.
/// </summary>
public class MainMenuPanel : UIMenu, IMainMenuView
{
    [SerializeField] private TextMeshProUGUI versionText;
    [SerializeField] private TextMeshProUGUI coinCountText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button resetSaveButton;
    [SerializeField] private Toggle debugLoggingToggle;
    [SerializeField] private SettingsPanel settingsPanel;
    [SerializeField] private ConfirmationDialog confirmationDialog;
    
    private MenuStackManager menuStackManager;

    public event Action PlayClicked;
    public event Action SettingsClicked;
    public event Action ResetSaveClicked;
    public event Action<bool> DebugLoggingToggled;
    
    [Inject]
    public void Construct(MenuStackManager menuStackManager)
    {
        this.menuStackManager = menuStackManager;
    }
    
    protected override void Awake()
    {
        // Don't call base.Awake() - we want the main menu to be visible by default
        if (menuPanel == null)
        {
            menuPanel = gameObject;
        }
        
        menuType = MenuType.PauseMenu;
        
        // Main menu should be open by default
        if (menuPanel != null)
        {
            menuPanel.SetActive(true);
        }
        IsOpen = true;
        
        // Subscribe to button clicks via code
        playButton.onClick.AddListener(() => PlayClicked?.Invoke());
        settingsButton.onClick.AddListener(() => SettingsClicked?.Invoke());
        resetSaveButton.onClick.AddListener(() => ResetSaveClicked?.Invoke());

        if (debugLoggingToggle != null)
        {
            // Initialize toggle from saved PlayerPrefs value
            debugLoggingToggle.isOn = BoardDebugConfig.IsEnabled;
            debugLoggingToggle.onValueChanged.AddListener(OnDebugLoggingToggleChanged);
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        playButton.onClick.RemoveAllListeners();
        settingsButton.onClick.RemoveAllListeners();
        resetSaveButton.onClick.RemoveAllListeners();
        if (debugLoggingToggle != null)
        {
            debugLoggingToggle.onValueChanged.RemoveListener(OnDebugLoggingToggleChanged);
        }
    }

    public void SetCoinCount(int coins)
    {
        coinCountText.text = $"x {coins}";
    }

    public void SetLevelIndex(int levelIndex)
    {
        levelText.text = $"Level {levelIndex}";
    }

    public void SetVersion(string version)
    {
        if (versionText != null)
        {
            versionText.text = version;
        }
    }

    private void OnDebugLoggingToggleChanged(bool isOn)
    {
        BoardDebugConfig.IsEnabled = isOn;
        DebugLoggingToggled?.Invoke(isOn);
    }
}
