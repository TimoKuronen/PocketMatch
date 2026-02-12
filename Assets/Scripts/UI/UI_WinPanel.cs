using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

/// <summary>
/// Win panel menu that appears when player completes a level.
/// </summary>
public class UI_WinPanel : UIMenu
{
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private TextMeshProUGUI coinCountText;
    [SerializeField] private UI_ConfirmationDialog confirmationDialog;
    
    private MenuStackManager menuStackManager;
    private IAdsService adsService;
    private IScoreService scoreService;
    private IGameSessionService gameSessionService;
    
    [Inject]
    public void Construct(
        MenuStackManager menuStackManager,
        IAdsService adsService,
        IScoreService scoreService,
        IGameSessionService gameSessionService)
    {
        this.menuStackManager = menuStackManager;
        this.adsService = adsService;
        this.scoreService = scoreService;
        this.gameSessionService = gameSessionService;
    }
    
    protected override void Awake()
    {
        base.Awake();
        menuType = MenuType.WinMenu;
    }
    
    public override void Open()
    {
        base.Open();
        
        // Update coin count display
        if (coinCountText != null && scoreService != null)
        {
            coinCountText.text = $"x {scoreService.GetTotalScore()}";
        }
        
        // Hide next level button if level cap reached
        if (nextLevelButton != null && gameSessionService != null)
        {
            nextLevelButton.gameObject.SetActive(!gameSessionService.IsLevelCapReached);
        }
    }
    
    public void NextLevelButtonPressed()
    {
        if (menuStackManager != null)
        {
            menuStackManager.PopMenu();
        }
        Loader.ShowInterstitialThenContinue(adsService, Loader.GameScene.PlayScene);
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
