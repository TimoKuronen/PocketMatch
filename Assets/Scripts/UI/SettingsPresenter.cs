using System;
using VContainer.Unity;

public class SettingsPresenter : IStartable, IDisposable
{
    private readonly ISettingsView view;
    private readonly IMenu settingsMenu;
    private readonly MenuStackManager menuStackManager;
    private readonly IAudioService audioService;
    private readonly ConfirmationDialog confirmationDialog;

    public SettingsPresenter(
        ISettingsView view,
        MenuStackManager menuStackManager,
        IAudioService audioService,
        ConfirmationDialog confirmationDialog)
    {
        this.view = view;
        this.settingsMenu = view as IMenu;
        this.menuStackManager = menuStackManager;
        this.audioService = audioService;
        this.confirmationDialog = confirmationDialog;
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
        view.SetVersion(BuildInfo.FormatVersionLabel());
    }

    private void OnCloseClicked()
    {
        menuStackManager.PopMenu();
    }

    private void OnRetryClicked()
    {
        if (!menuStackManager.CanOpenMenu())
            return;

        confirmationDialog.Setup("Are you sure you want to retry this level?", () =>
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

        confirmationDialog.Setup("Are you sure you want to return to the main menu?", () =>
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
