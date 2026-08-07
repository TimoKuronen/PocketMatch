using System;
using VContainer.Unity;

public class GameHudPresenter : IStartable, IDisposable
{
    private readonly IGameHudView view;
    private readonly ILevelManager levelManager;
    private readonly IGameSessionService gameSessionService;
    private readonly IEconomyService economyService;
    private readonly ISaveService saveService;
    private readonly MenuStackManager menuStackManager;
    private readonly IWinView winView;
    private readonly ILoseView loseView;
    private readonly IPauseSettingsView settingsView;

    public GameHudPresenter(
        IGameHudView view,
        ILevelManager levelManager,
        IGameSessionService gameSessionService,
        IEconomyService economyService,
        ISaveService saveService,
        MenuStackManager menuStackManager,
        IWinView winView,
        ILoseView loseView,
        IPauseSettingsView settingsView)
    {
        this.view = view;
        this.levelManager = levelManager;
        this.gameSessionService = gameSessionService;
        this.economyService = economyService;
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
        levelManager.OnLevelContinued += OnLevelContinued;
        levelManager.OnLevelWon += OnLevelWon;
        levelManager.OnLevelLost += OnLevelLost;
        economyService.OnBalanceChanged += OnWalletBalanceChanged;

        if (GameSignals.IsSessionLoaded)
            InitializeAfterSessionLoaded();
    }

    private void InitializeAfterSessionLoaded()
    {
        var mapData = gameSessionService.CurrentMapData;
        var levelIndex = GameSignals.ActiveLevelIndex >= 0
            ? GameSignals.ActiveLevelIndex + 1
            : saveService.PlayerData.nextLevelIndex + 1;

        view.SetLevelIndex(levelIndex);
        view.InitializeVictoryConditions(mapData.VictoryConditions);
        view.ShowVictoryConditions();
        UpdateWalletBalance();
        UpdateMoves();
    }

    private void HandleVictoryConditionUpdate()
    {
        UpdateMoves();
        view.UpdateVictoryConditions(levelManager.VictoryConditions, levelManager.MovesRemaining);
    }

    private void OnLevelContinued()
    {
        view.ShowVictoryConditions();
        UpdateMoves();
        UpdateWalletBalance();
        view.UpdateVictoryConditions(levelManager.VictoryConditions, levelManager.MovesRemaining);
    }

    private void OnLevelWon()
    {
        view.HideVictoryConditions();
        if (winView is IMenu menu)
            menuStackManager.PushMenu(menu);
    }

    private void OnLevelLost()
    {
        UpdateWalletBalance();
        if (loseView is IMenu menu)
            menuStackManager.PushMenu(menu);
    }

    private void OnWalletBalanceChanged(int balance)
    {
        view.SetWalletBalance(balance);
    }

    private void OnSettingsClicked()
    {
        if (menuStackManager.HasMenuOfType(MenuType.PauseMenu))
        {
            menuStackManager.PopMenuOfType(MenuType.PauseMenu);
            return;
        }

        if (!menuStackManager.CanOpenMenu())
            return;

        if (settingsView is IMenu menu)
            menuStackManager.PushMenu(menu);
    }

    private void UpdateMoves()
    {
        view.SetMoves(levelManager.MovesRemaining);
    }

    private void UpdateWalletBalance()
    {
        view.SetWalletBalance(economyService.Balance);
    }

    public void Dispose()
    {
        view.SettingsClicked -= OnSettingsClicked;
        GameSignals.OnSessionLoaded -= InitializeAfterSessionLoaded;
        levelManager.OnVictoryConditionsUpdated -= HandleVictoryConditionUpdate;
        levelManager.OnLevelContinued -= OnLevelContinued;
        levelManager.OnLevelWon -= OnLevelWon;
        levelManager.OnLevelLost -= OnLevelLost;
        economyService.OnBalanceChanged -= OnWalletBalanceChanged;
    }
}
