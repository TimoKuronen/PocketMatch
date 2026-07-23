using System;
using VContainer.Unity;

public class LosePresenter : IStartable, IDisposable
{
    private readonly ILoseView view;
    private readonly MenuStackManager menuStackManager;
    private readonly ConfirmationDialog confirmationDialog;

    public LosePresenter(ILoseView view, MenuStackManager menuStackManager, ConfirmationDialog confirmationDialog)
    {
        this.view = view;
        this.menuStackManager = menuStackManager;
        this.confirmationDialog = confirmationDialog;
    }

    public void Start()
    {
        view.RestartClicked += OnRestartClicked;
        view.MainMenuClicked += OnMainMenuClicked;
    }

    private void OnRestartClicked()
    {
        if (GameSignals.ActiveLevelIndex >= 0)
            GameSignals.SetPendingLevelIndex(GameSignals.ActiveLevelIndex);

        menuStackManager.ClearStack();
        Loader.Restart();
    }

    private void OnMainMenuClicked()
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

    public void Dispose()
    {
        view.RestartClicked -= OnRestartClicked;
        view.MainMenuClicked -= OnMainMenuClicked;
    }
}