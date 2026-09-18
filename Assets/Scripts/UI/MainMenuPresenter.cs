using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using VContainer.Unity;

/// <summary>
/// Main-menu MVP presenter; settings opens as a toggle, and the banner ad waits for ad SDK readiness.
/// </summary>
public class MainMenuPresenter : IStartable, IDisposable
{
    private readonly IMainMenuView view;
    private readonly IEconomyService economyService;
    private readonly IAdsService adsService;
    private readonly MenuStackManager menuStackManager;
    private readonly ILevelSelectView levelSelectView;
    private readonly IMainMenuSettingsView settingsView;
    private readonly ILocalizationService localization;
    private readonly ISaveService saveService;

    public MainMenuPresenter(
        IMainMenuView view,
        IEconomyService economyService,
        IAdsService adsService,
        MenuStackManager menuStackManager,
        ILevelSelectView levelSelectView,
        IMainMenuSettingsView settingsView,
        ILocalizationService localization,
        ISaveService saveService)
    {
        this.view = view;
        this.economyService = economyService;
        this.adsService = adsService;
        this.menuStackManager = menuStackManager;
        this.levelSelectView = levelSelectView;
        this.settingsView = settingsView;
        this.localization = localization;
        this.saveService = saveService;
    }

    public void Start()
    {
        view.PlayClicked += OnPlayClicked;
        view.SettingsClicked += OnSettingsClicked;
        economyService.OnBalanceChanged += OnBalanceChanged;
        localization.LocaleChanged += OnLocaleChanged;
        saveService.CloudSyncStatusChanged += OnCloudSyncStatusChanged;

        InitializeView();
        ShowBannerWhenReadyAsync().Forget();
    }

    private void InitializeView()
    {
        if (!localization.IsReady)
            return;

        view.SetCoinCount(economyService.Balance);
        RefreshFooter();
    }

    private void OnLocaleChanged()
    {
        if (!localization.IsReady)
            return;

        RefreshFooter();
        view.SetCoinCount(economyService.Balance);
    }

    private void OnCloudSyncStatusChanged()
    {
        if (!localization.IsReady)
            return;

        RefreshFooter();
    }

    private void RefreshFooter()
    {
        string version = localization.Get(
            LocalizationKeys.CommonVersionBuild,
            Application.version,
            BuildInfo.AndroidVersionCode);
        string cloud = localization.Get(CloudSyncStatusLabels.ToKey(saveService.CloudSyncStatus));
        view.SetVersion(localization.Get(LocalizationKeys.CommonSettingsFooter, version, cloud));
    }

    private async UniTaskVoid ShowBannerWhenReadyAsync()
    {
        var token = UnityEngine.Object.FindFirstObjectByType<MainMenuPanel>()?.GetCancellationTokenOnDestroy() ?? default;

        if (token.CanBeCanceled)
        {
            await UniTask.WaitUntil(() => adsService.IsInitialized, cancellationToken: token);
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: token);
        }
        else
        {
            await UniTask.WaitUntil(() => adsService.IsInitialized);
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
        }

        adsService.ShowBannerAd();
    }

    private void OnPlayClicked()
    {
        if (levelSelectView is not IMenu levelSelectMenu)
        {
            UnityEngine.Debug.LogError("[MainMenuPresenter] Level select panel is missing.");
            return;
        }

        menuStackManager.PushMenu(levelSelectMenu);
    }

    private void OnSettingsClicked()
    {
        if (settingsView is not IMenu settingsMenu)
        {
            UnityEngine.Debug.LogError("[MainMenuPresenter] Settings panel is missing.");
            return;
        }

        if (menuStackManager.HasMenuOfType(MenuType.SettingsMenu))
        {
            menuStackManager.PopMenuOfType(MenuType.SettingsMenu);
            return;
        }

        menuStackManager.PushMenu(settingsMenu);
    }

    private void OnBalanceChanged(int balance)
    {
        if (!localization.IsReady)
            return;

        view.SetCoinCount(balance);
    }

    public void Dispose()
    {
        view.PlayClicked -= OnPlayClicked;
        view.SettingsClicked -= OnSettingsClicked;
        economyService.OnBalanceChanged -= OnBalanceChanged;
        localization.LocaleChanged -= OnLocaleChanged;
        saveService.CloudSyncStatusChanged -= OnCloudSyncStatusChanged;
    }
}
