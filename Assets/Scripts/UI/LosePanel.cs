using UnityEngine;
using UnityEngine.UI;
using VContainer;

/// <summary>
/// Lose panel menu that appears when player fails a level.
/// Acts as a View in the MVP pattern.
/// </summary>
public class LosePanel : UIMenu, ILoseView
{
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private ConfirmationDialog confirmationDialog;
    
    public event System.Action RestartClicked;
    public event System.Action MainMenuClicked;

    [Inject]
    public void Construct() { }
    
    protected override void Awake()
    {
        base.Awake();
        menuType = MenuType.LoseMenu;
        
        // Subscribe to button clicks via code
        restartButton.onClick.AddListener(() => RestartClicked?.Invoke());
        mainMenuButton.onClick.AddListener(() => MainMenuClicked?.Invoke());
    }
    
    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        restartButton.onClick.RemoveAllListeners();
        mainMenuButton.onClick.RemoveAllListeners();
    }
}
