using System;
using VContainer.Unity;

public class PauseSettingsPresenter : IStartable, IDisposable
{
    private readonly IPauseSettingsView view;
    private readonly IMenu settingsMenu;
    private readonly MenuStackManager menuStackManager;
    private readonly IAudioService audioService;
    private readonly ConfirmationDialog confirmationDialog;
    private readonly ILocalizationService localization;

    public PauseSettingsPresenter(
        IPauseSettingsView view,
        MenuStackManager menuStackManager,
        IAudioService audioService,
        ConfirmationDialog confirmationDialog,
        ILocalizationService localization)
    {
        this.view = view;
        this.settingsMenu = view as IMenu;
        this.menuStackManager = menuStackManager;
        this.audioService = audioService;
        this.confirmationDialog = confirmationDialog;
        this.localization = localization;
    }

    public void Start()
    {
        if (settingsMenu != null)
            settingsMenu.OnMenuOpened += OnSettingsOpened;

        view.CloseClicked += OnCloseClicked;
        view.RetryClicked += OnRetryClicked;
        view.MenuClicked += OnMenuClicked;
        view.SfxVolumeChanged += OnSfxVolumeChanged;
    }

    private void OnSettingsOpened()
    {
        view.SetSfxVolume(audioService.SfxVolume);
    }

    private void OnCloseClicked()
    {
        menuStackManager.PopMenu();
    }

    private void OnRetryClicked()
    {
        if (!menuStackManager.CanOpenMenu())
            return;

        confirmationDialog.Setup(localization.Get(LocalizationKeys.PauseConfirmRetry), () =>
        {
            if (GameSignals.ActiveLevelIndex >= 0)
                GameSignals.SetPendingLevelIndex(GameSignals.ActiveLevelIndex);

            menuStackManager.ClearStack();
            Loader.Restart();
        });
        menuStackManager.PushMenu(confirmationDialog);
    }

    private void OnMenuClicked()
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

    private void OnSfxVolumeChanged(float value)
    {
        audioService.SfxVolume = value;
    }

    public void Dispose()
    {
        if (settingsMenu != null)
            settingsMenu.OnMenuOpened -= OnSettingsOpened;

        view.CloseClicked -= OnCloseClicked;
        view.RetryClicked -= OnRetryClicked;
        view.MenuClicked -= OnMenuClicked;
        view.SfxVolumeChanged -= OnSfxVolumeChanged;
    }
}
