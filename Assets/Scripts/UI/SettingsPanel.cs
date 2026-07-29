using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class SettingsPanel : UIMenu, ISettingsView
{
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

        closeButton.onClick.AddListener(() => CloseClicked?.Invoke());
        
        if (retryButton != null)
        {
            retryButton.onClick.AddListener(() => RetryClicked?.Invoke());
        }
        
        if (menuButton != null)
        {
            menuButton.onClick.AddListener(() => MenuClicked?.Invoke());
        }

        sfxSlider.onValueChanged.AddListener(value => SfxVolumeChanged?.Invoke(value));
    }

    public void SetSfxVolume(float value)
    {
        if (sfxSlider != null)
            sfxSlider.SetValueWithoutNotify(Mathf.Clamp01(value));
    }
    
    protected override void OnDestroy()
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

        if (retryButton != null)
            retryButton.gameObject.SetActive(contextToApply == SettingsContext.InGame);

        if (menuButton != null)
            menuButton.gameObject.SetActive(contextToApply == SettingsContext.InGame);
    }

    public override void Open()
    {
        ConfigureForContext(context);
        base.Open();
    }
}
