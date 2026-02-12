using UnityEngine;
using VContainer;

/// <summary>
/// Lose panel menu that appears when player fails a level.
/// </summary>
public class UI_LosePanel : UIMenu
{
    [SerializeField] private UnityEngine.UI.Button restartButton;
    [SerializeField] private UnityEngine.UI.Button mainMenuButton;
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
        menuType = MenuType.LoseMenu;
    }
    
    public void RestartButtonPressed()
    {
        if (menuStackManager != null)
        {
            menuStackManager.ClearStack();
        }
        Loader.Restart();
    }
    
    public void MainMenuButtonPressed()
    {
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
}
