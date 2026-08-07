using System;
using VContainer.Unity;

public class LosePresenter : IStartable, IDisposable
{
    private readonly ILoseView view;
    private readonly MenuStackManager menuStackManager;
    private readonly ConfirmationDialog confirmationDialog;
    private readonly ILevelContinueService levelContinueService;
    private readonly IEconomyService economyService;

    public LosePresenter(
        ILoseView view,
        MenuStackManager menuStackManager,
        ConfirmationDialog confirmationDialog,
        ILevelContinueService levelContinueService,
        IEconomyService economyService)
    {
        this.view = view;
        this.menuStackManager = menuStackManager;
        this.confirmationDialog = confirmationDialog;
        this.levelContinueService = levelContinueService;
        this.economyService = economyService;
    }

    public void Start()
    {
        view.RestartClicked += OnRestartClicked;
        view.MainMenuClicked += OnMainMenuClicked;
        view.ContinueWithCoinsClicked += OnContinueWithCoinsClicked;
        view.ContinueWithAdClicked += OnContinueWithAdClicked;
        view.Opened += RefreshContinueOptions;
        economyService.OnBalanceChanged += OnBalanceChanged;
    }

    private void RefreshContinueOptions()
    {
        view.SetWalletBalance(economyService.Balance);
        view.SetContinueWithCoinsAvailable(levelContinueService.CanContinueWithCoins);
        view.SetContinueWithAdAvailable(false);
    }

    private void OnBalanceChanged(int balance)
    {
        view.SetWalletBalance(balance);
        view.SetContinueWithCoinsAvailable(levelContinueService.CanContinueWithCoins);
    }

    private void OnContinueWithCoinsClicked()
    {
        if (!levelContinueService.TryContinueWithCoins())
            return;

        view.SetWalletBalance(economyService.Balance);
        menuStackManager.PopMenu();
    }

    private void OnContinueWithAdClicked()
    {
    }

    private void OnRestartClicked()
    {
        if (GameSignals.ActiveLevelIndex >= 0)
            GameSignals.SetPendingLevelIndex(GameSignals.ActiveLevelIndex);

        menuStackManager.ClearStack();
        Loader.Restart();
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
        view.RestartClicked -= OnRestartClicked;
        view.MainMenuClicked -= OnMainMenuClicked;
        view.ContinueWithCoinsClicked -= OnContinueWithCoinsClicked;
        view.ContinueWithAdClicked -= OnContinueWithAdClicked;
        view.Opened -= RefreshContinueOptions;
        economyService.OnBalanceChanged -= OnBalanceChanged;
    }
}
