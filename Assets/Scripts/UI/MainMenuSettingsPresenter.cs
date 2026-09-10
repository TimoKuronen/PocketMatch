using System;
using VContainer.Unity;

public class MainMenuSettingsPresenter : IStartable, IDisposable
{
    private readonly IMainMenuSettingsView view;
    private readonly IMenu settingsMenu;
    private readonly MenuStackManager menuStackManager;
    private readonly IAudioService audioService;
    private readonly ILocalizationService localization;

    public MainMenuSettingsPresenter(
        IMainMenuSettingsView view,
        MenuStackManager menuStackManager,
        IAudioService audioService,
        ILocalizationService localization)
    {
        this.view = view;
        this.settingsMenu = view as IMenu;
        this.menuStackManager = menuStackManager;
        this.audioService = audioService;
        this.localization = localization;
    }

    public void Start()
    {
        if (settingsMenu != null)
            settingsMenu.OnMenuOpened += OnSettingsOpened;

        view.CloseClicked += OnCloseClicked;
        view.SfxVolumeChanged += OnSfxVolumeChanged;
        localization.LocaleChanged += OnLocaleChanged;
    }

    private void OnSettingsOpened()
    {
        view.SetSfxVolume(audioService.SfxVolume);
        RefreshVersion();
    }

    private void OnLocaleChanged()
    {
        if (settingsMenu != null && settingsMenu.IsOpen)
            RefreshVersion();
    }

    private void RefreshVersion()
    {
        view.SetVersion(localization.Get(
            LocalizationKeys.CommonVersionBuild,
            UnityEngine.Application.version,
            BuildInfo.AndroidVersionCode));
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
        localization.LocaleChanged -= OnLocaleChanged;
    }
}
