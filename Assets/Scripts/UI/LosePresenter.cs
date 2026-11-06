using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

public class LosePresenter : IStartable, IDisposable
{
    private readonly ILoseView view;
    private readonly MenuStackManager menuStackManager;
    private readonly ConfirmationDialog confirmationDialog;
    private readonly ILevelContinueService levelContinueService;
    private readonly IEconomyService economyService;
    private readonly ILocalizationService localization;
    private readonly CancellationTokenSource disposeCts = new();

    public LosePresenter(
        ILoseView view,
        MenuStackManager menuStackManager,
        ConfirmationDialog confirmationDialog,
        ILevelContinueService levelContinueService,
        IEconomyService economyService,
        ILocalizationService localization)
    {
        this.view = view;
        this.menuStackManager = menuStackManager;
        this.confirmationDialog = confirmationDialog;
        this.levelContinueService = levelContinueService;
        this.economyService = economyService;
        this.localization = localization;
    }

    public void Start()
    {
        view.RestartClicked += OnRestartClicked;
        view.MainMenuClicked += OnMainMenuClicked;
        view.ContinueWithCoinsClicked += OnContinueWithCoinsClicked;
        view.ContinueWithAdClicked += OnContinueWithAdClicked;
        view.Opened += RefreshContinueOptions;
        economyService.OnBalanceChanged += OnBalanceChanged;
        localization.LocaleChanged += OnLocaleChanged;
    }

    private void OnLocaleChanged()
    {
        if (view is IMenu menu && !menu.IsOpen)
            return;

        RefreshContinueOptions();
    }

    private void RefreshContinueOptions()
    {
        view.SetWalletBalance(economyService.Balance);
        view.SetContinueWithCoinsAvailable(levelContinueService.CanContinueWithCoins);
        view.SetContinueWithAdAvailable(levelContinueService.CanContinueWithAd);
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
        ContinueWithAdAsync().Forget();
    }

    private async UniTaskVoid ContinueWithAdAsync()
    {
        if (!levelContinueService.CanContinueWithAd)
            return;

        view.SetContinueWithCoinsAvailable(false);
        view.SetContinueWithAdAvailable(false);

        AdContinueResult result;
        try
        {
            result = await levelContinueService.TryContinueWithAdAsync(disposeCts.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (result == AdContinueResult.Continued)
        {
            menuStackManager.PopMenu();
            return;
        }

        RefreshContinueOptions();

        // No ad to show (no fill / not ready): keep the ad option off for this lose screen.
        if (result == AdContinueResult.Unavailable)
            view.SetContinueWithAdAvailable(false);
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

        confirmationDialog.Setup(localization.Get(LocalizationKeys.CommonConfirmMainMenu), () =>
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
        localization.LocaleChanged -= OnLocaleChanged;

        disposeCts.Cancel();
        disposeCts.Dispose();
    }
}
