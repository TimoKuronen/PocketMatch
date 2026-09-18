using System;
using VContainer.Unity;

public class MainMenuSettingsPresenter : IStartable, IDisposable
{
    private readonly IMainMenuSettingsView view;
    private readonly IMenu settingsMenu;
    private readonly MenuStackManager menuStackManager;
    private readonly IAudioService audioService;
    private readonly ILocalizationService localization;
    private readonly ISaveService saveService;

    public MainMenuSettingsPresenter(
        IMainMenuSettingsView view,
        MenuStackManager menuStackManager,
        IAudioService audioService,
        ILocalizationService localization,
        ISaveService saveService)
    {
        this.view = view;
        this.settingsMenu = view as IMenu;
        this.menuStackManager = menuStackManager;
        this.audioService = audioService;
        this.localization = localization;
        this.saveService = saveService;
    }

    public void Start()
    {
        if (settingsMenu != null)
            settingsMenu.OnMenuOpened += OnSettingsOpened;

        view.CloseClicked += OnCloseClicked;
        view.SfxVolumeChanged += OnSfxVolumeChanged;
        localization.LocaleChanged += OnLocaleChanged;
        saveService.CloudSyncStatusChanged += OnCloudSyncStatusChanged;
    }

    private void OnSettingsOpened()
    {
        view.SetSfxVolume(audioService.SfxVolume);
        RefreshFooter();
    }

    private void OnLocaleChanged()
    {
        if (settingsMenu != null && settingsMenu.IsOpen)
            RefreshFooter();
    }

    private void OnCloudSyncStatusChanged()
    {
        if (settingsMenu != null && settingsMenu.IsOpen)
            RefreshFooter();
    }

    private void RefreshFooter()
    {
        string version = localization.Get(
            LocalizationKeys.CommonVersionBuild,
            UnityEngine.Application.version,
            BuildInfo.AndroidVersionCode);
        string cloud = CloudSyncStatusLabels.ToDisplay(saveService.CloudSyncStatus);
        view.SetVersion($"{version}\n{cloud}");
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
        saveService.CloudSyncStatusChanged -= OnCloudSyncStatusChanged;
    }
}
