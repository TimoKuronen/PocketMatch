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
    [SerializeField] private SettingsPanel settingsPanel;
    [SerializeField] private ConfirmationDialog confirmationDialog;
    
    private MenuStackManager menuStackManager;

    public event Action PlayClicked;
    public event Action SettingsClicked;
    public event Action ResetSaveClicked;
    
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
    }
    
    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        playButton.onClick.RemoveAllListeners();
        settingsButton.onClick.RemoveAllListeners();
        resetSaveButton.onClick.RemoveAllListeners();
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
}
