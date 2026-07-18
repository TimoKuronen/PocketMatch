using System;
using VContainer.Unity;

public class SettingsPresenter : IStartable, IDisposable
{
    private readonly ISettingsView view;
    private readonly MenuStackManager menuStackManager;
    private readonly IAudioService audioService;

    public SettingsPresenter(ISettingsView view, MenuStackManager menuStackManager, IAudioService audioService)
    {
        this.view = view;
        this.menuStackManager = menuStackManager;
        this.audioService = audioService;
    }

    public void Start()
    {
        view.SetSfxVolume(audioService.SfxVolume);
        view.CloseClicked += OnCloseClicked;
        view.RetryClicked += OnRetryClicked;
        view.MenuClicked += OnMenuClicked;
        view.SfxVolumeChanged += OnSfxVolumeChanged;
    }

    private void OnCloseClicked()
    {
        menuStackManager.PopMenu();
    }

    private void OnRetryClicked()
    {
        menuStackManager.ClearStack();
        Loader.Restart();
    }

    private void OnMenuClicked()
    {
        if (!menuStackManager.CanOpenMenu())
            return;
    }

    private void OnSfxVolumeChanged(float value)
    {
        audioService.SfxVolume = value;
    }

    public void Dispose()
    {
        view.CloseClicked -= OnCloseClicked;
        view.RetryClicked -= OnRetryClicked;
        view.MenuClicked -= OnMenuClicked;
        view.SfxVolumeChanged -= OnSfxVolumeChanged;
    }
}
