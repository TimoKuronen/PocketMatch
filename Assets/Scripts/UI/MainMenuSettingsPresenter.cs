using System;
using VContainer.Unity;

public class MainMenuSettingsPresenter : IStartable, IDisposable
{
    private readonly IMainMenuSettingsView view;
    private readonly IMenu settingsMenu;
    private readonly MenuStackManager menuStackManager;
    private readonly IAudioService audioService;
    private readonly IAnalyticsConsent analyticsConsent;

    public MainMenuSettingsPresenter(
        IMainMenuSettingsView view,
        MenuStackManager menuStackManager,
        IAudioService audioService,
        IAnalyticsConsent analyticsConsent)
    {
        this.view = view;
        this.settingsMenu = view as IMenu;
        this.menuStackManager = menuStackManager;
        this.audioService = audioService;
        this.analyticsConsent = analyticsConsent;
    }

    public void Start()
    {
        if (settingsMenu != null)
            settingsMenu.OnMenuOpened += OnSettingsOpened;

        view.CloseClicked += OnCloseClicked;
        view.SfxVolumeChanged += OnSfxVolumeChanged;
        view.AnalyticsConsentChanged += OnAnalyticsConsentChanged;
    }

    private void OnSettingsOpened()
    {
        view.SetSfxVolume(audioService.SfxVolume);
        view.SetAnalyticsConsent(analyticsConsent.State == AnalyticsConsentState.Granted);
    }

    private void OnCloseClicked()
    {
        menuStackManager.PopMenu();
    }

    private void OnSfxVolumeChanged(float value)
    {
        audioService.SfxVolume = value;
    }

    private void OnAnalyticsConsentChanged(bool granted)
    {
        analyticsConsent.SetGranted(granted);
    }

    public void Dispose()
    {
        if (settingsMenu != null)
            settingsMenu.OnMenuOpened -= OnSettingsOpened;

        view.CloseClicked -= OnCloseClicked;
        view.SfxVolumeChanged -= OnSfxVolumeChanged;
        view.AnalyticsConsentChanged -= OnAnalyticsConsentChanged;
    }
}
