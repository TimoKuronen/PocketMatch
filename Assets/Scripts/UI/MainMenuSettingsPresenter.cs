using System;
using VContainer.Unity;

public class MainMenuSettingsPresenter : IStartable, IDisposable
{
    private readonly IMainMenuSettingsView view;
    private readonly IMenu settingsMenu;
    private readonly MenuStackManager menuStackManager;
    private readonly IAudioService audioService;

    public MainMenuSettingsPresenter(
        IMainMenuSettingsView view,
        MenuStackManager menuStackManager,
        IAudioService audioService)
    {
        this.view = view;
        this.settingsMenu = view as IMenu;
        this.menuStackManager = menuStackManager;
        this.audioService = audioService;
    }

    public void Start()
    {
        if (settingsMenu != null)
            settingsMenu.OnMenuOpened += OnSettingsOpened;

        view.CloseClicked += OnCloseClicked;
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

    private void OnSfxVolumeChanged(float value)
    {
        audioService.SfxVolume = value;
    }

    public void Dispose()
    {
        if (settingsMenu != null)
            settingsMenu.OnMenuOpened -= OnSettingsOpened;

        view.CloseClicked -= OnCloseClicked;
        view.SfxVolumeChanged -= OnSfxVolumeChanged;
    }
}
