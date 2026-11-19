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
    private readonly ILocalizationService localization;
    private readonly ITunables tunables;

    public WinPresenter(
        IWinView view,
        MenuStackManager menuStackManager,
        IAdsService adsService,
        ILevelEarningsService levelEarningsService,
        ILevelManager levelManager,
        IGameSessionService gameSessionService,
        ISaveService saveService,
        ConfirmationDialog confirmationDialog,
        ILocalizationService localization,
        ITunables tunables)
    {
        this.view = view;
        this.menuStackManager = menuStackManager;
        this.adsService = adsService;
        this.levelEarningsService = levelEarningsService;
        this.levelManager = levelManager;
        this.gameSessionService = gameSessionService;
        this.saveService = saveService;
        this.confirmationDialog = confirmationDialog;
        this.localization = localization;
        this.tunables = tunables;
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
        GameSignals.SetPendingLevelIndex(GameSignals.ActiveLevelIndex + 1);
        LoadScene(Loader.GameScene.PlayScene);
    }

    private void OnMainMenuClicked()
    {
        if (!menuStackManager.CanOpenMenu())
            return;

        confirmationDialog.Setup(localization.Get(LocalizationKeys.CommonConfirmMainMenu), () =>
        {
            menuStackManager.ClearStack();
            LoadScene(Loader.GameScene.MainMenu);
        });
        menuStackManager.PushMenu(confirmationDialog);
    }

    private void LoadScene(Loader.GameScene scene)
    {
        if (tunables.InterstitialsEnabled)
            Loader.ShowInterstitialThenContinue(adsService, scene);
        else
            Loader.Load(scene);
    }

    public void Dispose()
    {
        view.NextLevelClicked -= OnNextLevelClicked;
        view.MainMenuClicked -= OnMainMenuClicked;
        levelManager.OnLevelWon -= OnLevelWon;
    }
}
