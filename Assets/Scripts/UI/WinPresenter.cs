using System;
using VContainer.Unity;

public class WinPresenter : IStartable, IDisposable
{
    private readonly IWinView view;
    private readonly MenuStackManager menuStackManager;
    private readonly IAdsService adsService;
    private readonly IScoreService scoreService;
    private readonly IGameSessionService gameSessionService;
    private readonly ConfirmationDialog confirmationDialog;

    public WinPresenter(
        IWinView view,
        MenuStackManager menuStackManager,
        IAdsService adsService,
        IScoreService scoreService,
        IGameSessionService gameSessionService,
        ConfirmationDialog confirmationDialog)
    {
        this.view = view;
        this.menuStackManager = menuStackManager;
        this.adsService = adsService;
        this.scoreService = scoreService;
        this.gameSessionService = gameSessionService;
        this.confirmationDialog = confirmationDialog;
    }

    public void Start()
    {
        view.NextLevelClicked += OnNextLevelClicked;
        view.MainMenuClicked += OnMainMenuClicked;

        InitializeView();
    }

    private void InitializeView()
    {
        view.SetCoinCount(scoreService.GetTotalScore());
        view.SetNextLevelButtonVisible(!gameSessionService.IsLevelCapReached);
    }

    private void OnNextLevelClicked()
    {
        menuStackManager.PopMenu();
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
    }
}