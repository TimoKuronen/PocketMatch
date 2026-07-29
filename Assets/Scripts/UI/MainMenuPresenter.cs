using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using VContainer.Unity;

public class MainMenuPresenter : IStartable, IDisposable
{
    private readonly IMainMenuView view;
    private readonly ISaveService saveService;
    private readonly IAdsService adsService;
    private readonly MenuStackManager menuStackManager;
    private readonly ILevelSelectView levelSelectView;
    private readonly ISettingsView settingsView;

    public MainMenuPresenter(
        IMainMenuView view,
        ISaveService saveService,
        IAdsService adsService,
        MenuStackManager menuStackManager,
        ILevelSelectView levelSelectView,
        ISettingsView settingsView)
    {
        this.view = view;
        this.saveService = saveService;
        this.adsService = adsService;
        this.menuStackManager = menuStackManager;
        this.levelSelectView = levelSelectView;
        this.settingsView = settingsView;
    }

    public void Start()
    {
        view.PlayClicked += OnPlayClicked;
        view.SettingsClicked += OnSettingsClicked;
        view.ResetSaveClicked += OnResetSaveClicked;

        InitializeView();
        ShowBannerWhenReadyAsync().Forget();
    }

    private void InitializeView()
    {
        var playerData = saveService.PlayerData;

        view.SetCoinCount(playerData.coins);
        view.SetVersion($"v{UnityEngine.Application.version}");
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

        settingsView.ConfigureForContext(SettingsContext.MainMenu);
        menuStackManager.PushMenu(settingsMenu);
    }

    private void OnResetSaveClicked()
    {
        saveService.ResetToDefaults();
        view.SetCoinCount(saveService.PlayerData.coins);
    }

    public void Dispose()
    {
        view.PlayClicked -= OnPlayClicked;
        view.SettingsClicked -= OnSettingsClicked;
        view.ResetSaveClicked -= OnResetSaveClicked;
    }
}
