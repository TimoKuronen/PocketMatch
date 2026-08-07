using System;
using VContainer.Unity;

public class WinPresenter : IStartable, IDisposable
{
    private readonly IWinView view;
    private readonly MenuStackManager menuStackManager;
    private readonly IAdsService adsService;
    private readonly ILevelEarningsService levelEarningsService;
    private readonly ILevelManager levelManager;
    private readonly IGameSessionService gameSessionService;
    private readonly ISaveService saveService;
    private readonly ConfirmationDialog confirmationDialog;

    public WinPresenter(
        IWinView view,
        MenuStackManager menuStackManager,
        IAdsService adsService,
        ILevelEarningsService levelEarningsService,
        ILevelManager levelManager,
        IGameSessionService gameSessionService,
        ISaveService saveService,
        ConfirmationDialog confirmationDialog)
    {
        this.view = view;
        this.menuStackManager = menuStackManager;
        this.adsService = adsService;
        this.levelEarningsService = levelEarningsService;
        this.levelManager = levelManager;
        this.gameSessionService = gameSessionService;
        this.saveService = saveService;
        this.confirmationDialog = confirmationDialog;
    }

    public void Start()
    {
        view.NextLevelClicked += OnNextLevelClicked;
        view.MainMenuClicked += OnMainMenuClicked;
        levelManager.OnLevelWon += OnLevelWon;
    }

    private void OnLevelWon()
    {
        var earnings = levelEarningsService.GetLevelEarnings(levelManager.MovesRemaining).Total;
        view.SetEarnedCoins(earnings);
        view.SetNextLevelButtonVisible(!gameSessionService.IsLevelCapReached);
    }

    private void OnNextLevelClicked()
    {
        menuStackManager.PopMenu();
        GameSignals.SetPendingLevelIndex(saveService.PlayerData.nextLevelIndex);
        Loader.ShowInterstitialThenContinue(adsService, Loader.GameScene.PlayScene);
    }

    private void OnMainMenuClicked()
    {
        if (!menuStackManager.CanOpenMenu())
            return;

        confirmationDialog.Setup("Are you sure you want to return to the main menu?", () =>
        {
            menuStackManager.ClearStack();
            Loader.Load(Loader.GameScene.MainMenu);
        });
        menuStackManager.PushMenu(confirmationDialog);
    }

    public void Dispose()
    {
        view.NextLevelClicked -= OnNextLevelClicked;
        view.MainMenuClicked -= OnMainMenuClicked;
        levelManager.OnLevelWon -= OnLevelWon;
    }
}
