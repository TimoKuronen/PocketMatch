using UnityEngine;
using UnityEngine.UI;
using VContainer;

/// <summary>
/// Unified settings panel that works for both main menu and in-game contexts.
/// Acts as a View in the MVP pattern.
/// </summary>
public class SettingsPanel : UIMenu, ISettingsView
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

    public event System.Action CloseClicked;
    public event System.Action RetryClicked;
    public event System.Action MenuClicked;
    public event System.Action<float> SfxVolumeChanged;

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
        ConfigureForContext(context);
        
        // Subscribe to button clicks via code
        closeButton.onClick.AddListener(() => CloseClicked?.Invoke());
        
        if (retryButton != null)
        {
            retryButton.onClick.AddListener(() => RetryClicked?.Invoke());
        }
        
        if (menuButton != null)
        {
            menuButton.onClick.AddListener(() => MenuClicked?.Invoke());
        }
        
        // Initialize SFX slider (presenter may overwrite with saved volume in Start)
        sfxSlider.value = 1.0f;
        sfxSlider.onValueChanged.AddListener(value => SfxVolumeChanged?.Invoke(value));
    }

    public void SetSfxVolume(float value)
    {
        if (sfxSlider != null)
            sfxSlider.SetValueWithoutNotify(Mathf.Clamp01(value));
    }
    
    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        closeButton.onClick.RemoveAllListeners();
        
        if (retryButton != null)
        {
            retryButton.onClick.RemoveAllListeners();
        }
        
        if (menuButton != null)
        {
            menuButton.onClick.RemoveAllListeners();
        }
        
        sfxSlider.onValueChanged.RemoveAllListeners();
        base.OnDestroy();
    }
    
    public void ConfigureForContext(SettingsContext contextToApply)
    {
        context = contextToApply;
        // Hide/show buttons based on context
        if (retryButton != null)
        {
            retryButton.gameObject.SetActive(contextToApply == SettingsContext.InGame);
        }
        
        if (menuButton != null)
        {
            menuButton.gameObject.SetActive(contextToApply == SettingsContext.InGame);
        }
    }
}
