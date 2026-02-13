using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

/// <summary>
/// Win panel menu that appears when player completes a level.
/// </summary>
public class WinPanel : UIMenu
{
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private TextMeshProUGUI coinCountText;
    [SerializeField] private ConfirmationDialog confirmationDialog;
    
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
        
        // Subscribe to button clicks via code
        nextLevelButton.onClick.AddListener(OnNextLevelButtonClicked);
        mainMenuButton.onClick.AddListener(OnMainMenuButtonClicked);
    }
    
    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        nextLevelButton.onClick.RemoveListener(OnNextLevelButtonClicked);
        mainMenuButton.onClick.RemoveListener(OnMainMenuButtonClicked);
    }
    
    public override void Open()
    {
        base.Open();
        
        // Update coin count display
        coinCountText.text = $"x {scoreService.GetTotalScore()}";
        
        // Hide next level button if level cap reached
        nextLevelButton.gameObject.SetActive(!gameSessionService.IsLevelCapReached);
    }
    
    private void OnNextLevelButtonClicked()
    {
        menuStackManager.PopMenu();
        Loader.ShowInterstitialThenContinue(adsService, Loader.GameScene.PlayScene);
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
