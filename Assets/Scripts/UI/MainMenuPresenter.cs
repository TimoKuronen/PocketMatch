using Cysharp.Threading.Tasks;
using System;
using VContainer.Unity;

/// <summary>
/// Presenter for the main menu. Orchestrates between save data, ads, and the main menu view.
/// </summary>
public class MainMenuPresenter : IStartable
{
    private readonly IMainMenuView view;
    private readonly ISaveService saveService;
    private readonly IAdsService adsService;

    public MainMenuPresenter(
        IMainMenuView view,
        ISaveService saveService,
        IAdsService adsService)
    {
        this.view = view;
        this.saveService = saveService;
        this.adsService = adsService;
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
        var levelIndex = playerData.nextLevelIndex;

        view.SetCoinCount(playerData.coins);
        view.SetLevelIndex(levelIndex + 1);
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
        adsService.HideBannerAd();
        Loader.Load(Loader.GameScene.PlayScene);
    }

    private void OnSettingsClicked()
    {

    }

    private void OnResetSaveClicked()
    {
        saveService.ResetToDefaults();
        var playerData = saveService.PlayerData;
        var levelIndex = playerData.nextLevelIndex;

        view.SetCoinCount(playerData.coins);
        view.SetLevelIndex(levelIndex + 1);
    }

    private void OnDebugLoggingToggled(bool enabled)
    {
        // Nothing extra for now; the toggle already writes PlayerPrefs via BoardDebugConfig.
        // This hook exists so we can later tie analytics or UI feedback here if needed.
    }
}