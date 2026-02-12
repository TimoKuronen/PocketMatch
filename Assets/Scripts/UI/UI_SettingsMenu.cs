using UnityEngine;
using UnityEngine.UI;
using VContainer;

/// <summary>
/// Settings menu for in-game use with retry, menu, and SFX slider.
/// </summary>
public class UI_SettingsMenu : UIMenu
{
    [SerializeField] private Button retryButton;
    [SerializeField] private Button menuButton;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private UI_ConfirmationDialog confirmationDialog;
    
    private MenuStackManager menuStackManager;
    
    [Inject]
    public void Construct(MenuStackManager menuStackManager)
    {
        this.menuStackManager = menuStackManager;
    }
    
    protected override void Awake()
    {
        base.Awake();
        menuType = MenuType.SettingsMenu;
        
        // Subscribe to button clicks via code (professional approach)
        if (retryButton != null)
        {
            retryButton.onClick.AddListener(OnRetryButtonClicked);
        }
        
        if (menuButton != null)
        {
            menuButton.onClick.AddListener(OnMenuButtonClicked);
        }
        
        // Initialize SFX slider
        if (sfxSlider != null)
        {
            sfxSlider.value = 1.0f; // Default to full volume
            sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(OnRetryButtonClicked);
        }
        
        if (menuButton != null)
        {
            menuButton.onClick.RemoveListener(OnMenuButtonClicked);
        }
        
        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);
        }
    }
    
    private void OnRetryButtonClicked()
    {
        if (menuStackManager != null)
        {
            menuStackManager.ClearStack();
        }
        Loader.Restart();
    }
    
    private void OnMenuButtonClicked()
    {
        // Show confirmation dialog before leaving
        if (menuStackManager != null && confirmationDialog != null)
        {
            if (menuStackManager.CanOpenMenu())
            {
                confirmationDialog.Setup("Are you sure you want to return to the main menu?", () =>
                {
                    menuStackManager.ClearStack();
                    Loader.Load(Loader.GameScene.MainMenu);
                });
                menuStackManager.PushMenu(confirmationDialog);
            }
        }
    }
    
    public void CloseButtonPressed()
    {
        if (menuStackManager != null)
        {
            menuStackManager.PopMenu();
        }
    }
    
    private void OnSFXVolumeChanged(float value)
    {
        // TODO: Implement SFX volume control
        Debug.Log($"SFX Volume changed to: {value}");
    }
}
