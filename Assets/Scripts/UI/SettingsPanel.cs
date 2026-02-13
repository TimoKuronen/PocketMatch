using UnityEngine;
using UnityEngine.UI;
using VContainer;

/// <summary>
/// Unified settings panel that works for both main menu and in-game contexts.
/// </summary>
public class SettingsPanel : UIMenu
{
    public enum SettingsContext
    {
        MainMenu,
        InGame
    }
    
    [SerializeField] private SettingsContext context = SettingsContext.InGame;
    [SerializeField] private Button retryButton; // Only shown in InGame context
    [SerializeField] private Button menuButton; // Only shown in InGame context
    [SerializeField] private Button closeButton;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private ConfirmationDialog confirmationDialog;
    
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
        
        // Configure UI based on context
        ConfigureForContext();
        
        // Subscribe to button clicks via code
        closeButton.onClick.AddListener(OnCloseButtonClicked);
        
        if (retryButton != null)
        {
            retryButton.onClick.AddListener(OnRetryButtonClicked);
        }
        
        if (menuButton != null)
        {
            menuButton.onClick.AddListener(OnMenuButtonClicked);
        }
        
        // Initialize SFX slider
        sfxSlider.value = 1.0f; // Default to full volume
        sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
    }
    
    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        closeButton.onClick.RemoveListener(OnCloseButtonClicked);
        
        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(OnRetryButtonClicked);
        }
        
        if (menuButton != null)
        {
            menuButton.onClick.RemoveListener(OnMenuButtonClicked);
        }
        
        sfxSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);
    }
    
    private void ConfigureForContext()
    {
        // Hide/show buttons based on context
        if (retryButton != null)
        {
            retryButton.gameObject.SetActive(context == SettingsContext.InGame);
        }
        
        if (menuButton != null)
        {
            menuButton.gameObject.SetActive(context == SettingsContext.InGame);
        }
    }
    
    private void OnCloseButtonClicked()
    {
        menuStackManager.PopMenu();
    }
    
    private void OnRetryButtonClicked()
    {
        menuStackManager.ClearStack();
        Loader.Restart();
    }
    
    private void OnMenuButtonClicked()
    {
        // Show confirmation dialog before leaving
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
    
    private void OnSFXVolumeChanged(float value)
    {
        // TODO: Implement SFX volume control
        Debug.Log($"SFX Volume changed to: {value}");
    }
}
