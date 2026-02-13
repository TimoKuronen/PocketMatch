using UnityEngine;
using UnityEngine.UI;
using VContainer;

/// <summary>
/// Lose panel menu that appears when player fails a level.
/// </summary>
public class LosePanel : UIMenu
{
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
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
        menuType = MenuType.LoseMenu;
        
        // Subscribe to button clicks via code
        restartButton.onClick.AddListener(OnRestartButtonClicked);
        mainMenuButton.onClick.AddListener(OnMainMenuButtonClicked);
    }
    
    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        restartButton.onClick.RemoveListener(OnRestartButtonClicked);
        mainMenuButton.onClick.RemoveListener(OnMainMenuButtonClicked);
    }
    
    private void OnRestartButtonClicked()
    {
        menuStackManager.ClearStack();
        Loader.Restart();
    }
    
    private void OnMainMenuButtonClicked()
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
