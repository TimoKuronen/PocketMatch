using System;
using VContainer.Unity;

public class SettingsPresenter : IStartable, IDisposable
{
    private readonly ISettingsView view;
    private readonly MenuStackManager menuStackManager;

    public SettingsPresenter(ISettingsView view, MenuStackManager menuStackManager)
    {
        this.view = view;
        this.menuStackManager = menuStackManager;
    }

    public void Start()
    {
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
        // TODO: integrate with an audio/settings service once available.
        UnityEngine.Debug.Log($"SFX Volume changed to: {value}");
    }

    public void Dispose()
    {
        view.CloseClicked -= OnCloseClicked;
        view.RetryClicked -= OnRetryClicked;
        view.MenuClicked -= OnMenuClicked;
        view.SfxVolumeChanged -= OnSfxVolumeChanged;
    }
}