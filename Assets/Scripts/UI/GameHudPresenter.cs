using System;
using VContainer.Unity;

public class GameHudPresenter : IStartable, IDisposable
{
    private readonly IGameHudView view;
    private readonly ILevelManager levelManager;
    private readonly IGameSessionService gameSessionService;
    private readonly IScoreService scoreService;
    private readonly ISaveService saveService;
    private readonly MenuStackManager menuStackManager;
    private readonly IWinView winView;
    private readonly ILoseView loseView;
    private readonly ISettingsView settingsView;

    public GameHudPresenter(
        IGameHudView view,
        ILevelManager levelManager,
        IGameSessionService gameSessionService,
        IScoreService scoreService,
        ISaveService saveService,
        MenuStackManager menuStackManager,
        IWinView winView,
        ILoseView loseView,
        ISettingsView settingsView)
    {
        this.view = view;
        this.levelManager = levelManager;
        this.gameSessionService = gameSessionService;
        this.scoreService = scoreService;
        this.saveService = saveService;
        this.menuStackManager = menuStackManager;
        this.winView = winView;
        this.loseView = loseView;
        this.settingsView = settingsView;
    }

    public void Start()
    {
        view.SettingsClicked += OnSettingsClicked;

        GameSignals.OnSessionLoaded += InitializeAfterSessionLoaded;

        levelManager.OnVictoryConditionsUpdated += HandleVictoryConditionUpdate;
        levelManager.OnLevelWon += OnLevelWon;
        levelManager.OnLevelLost += OnLevelLost;
    }

    private void InitializeAfterSessionLoaded()
    {
        var mapData = gameSessionService.CurrentMapData;
        var levelIndex = GameSignals.ActiveLevelIndex >= 0
            ? GameSignals.ActiveLevelIndex + 1
            : saveService.PlayerData.nextLevelIndex + 1;

        view.SetLevelIndex(levelIndex);
        view.InitializeVictoryConditions(mapData.VictoryConditions);
        UpdateMoves();
        UpdateCoins(scoreService.GetTotalScore());
    }

    private void HandleVictoryConditionUpdate()
    {
        view.UpdateVictoryConditions(levelManager.VictoryConditions, levelManager.MovesRemaining);
    }

    private void OnLevelWon()
    {
        UpdateCoins(scoreService.GetTotalScore());
        view.HideVictoryConditions();
        if (winView is IMenu menu)
        {
            menuStackManager.PushMenu(menu);
        }
    }

    private void OnLevelLost()
    {
        view.HideVictoryConditions();
        if (loseView is IMenu menu)
        {
            menuStackManager.PushMenu(menu);
        }
    }

    private void OnSettingsClicked()
    {
        if (menuStackManager.HasMenuOfType(MenuType.SettingsMenu))
        {
            menuStackManager.PopMenuOfType(MenuType.SettingsMenu);
            return;
        }

        if (!menuStackManager.CanOpenMenu())
            return;

        settingsView.ConfigureForContext(SettingsContext.InGame);

        if (settingsView is IMenu menu)
        {
            menuStackManager.PushMenu(menu);
        }
    }

    private void UpdateMoves()
    {
        view.SetMoves(levelManager.MovesRemaining);
    }

    private void UpdateCoins(int coins)
    {
        view.SetCoinCount(coins);
    }

    public void Dispose()
    {
        view.SettingsClicked -= OnSettingsClicked;

        GameSignals.OnSessionLoaded -= InitializeAfterSessionLoaded;
        levelManager.OnVictoryConditionsUpdated -= HandleVictoryConditionUpdate;
        levelManager.OnLevelWon -= OnLevelWon;
        levelManager.OnLevelLost -= OnLevelLost;
    }
}