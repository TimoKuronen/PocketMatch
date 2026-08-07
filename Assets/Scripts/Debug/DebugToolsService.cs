using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public sealed class DebugToolsService : IDebugToolsService, IStartable, IDisposable
{
    public static event Action PanelRefreshRequested;

    private readonly ISaveService saveService;
    private readonly IEconomyService economyService;
    private readonly IAdsService adsService;
    private readonly List<IDebugAction> actions = new List<IDebugAction>();
    private readonly DebugContext context;
    private IDebugLevelTarget levelTarget;

    [Inject]
    public DebugToolsService(
        ISaveService saveService,
        IEconomyService economyService,
        IAdsService adsService)
    {
        this.saveService = saveService;
        this.economyService = economyService;
        this.adsService = adsService;
        context = new DebugContext(saveService, economyService, adsService, this);

        Register(new ForceWinDebugAction());
        Register(new ForceLoseDebugAction());
        Register(new ResetSaveDebugAction());
        Register(new SetCoinsDebugAction());
        Register(new AddCoinsDebugAction());
        Register(new ToggleBoardLoggingDebugAction());
        Register(new ShowBannerAdDebugAction());
        Register(new ShowInterstitialAdDebugAction());
    }

    public IReadOnlyList<IDebugAction> Actions => actions;
    public bool HasLevelTarget => levelTarget != null;

    public void Start()
    {
        if (!DebugTools.IsEnabled)
            return;

        ApplyStartingCoinsOverride();
    }

    public void RegisterLevelTarget(IDebugLevelTarget target)
    {
        levelTarget = target;
        RefreshPanel();
    }

    public void UnregisterLevelTarget(IDebugLevelTarget target)
    {
        if (levelTarget == target)
            levelTarget = null;

        RefreshPanel();
    }

    public bool TryExecute(string actionId, int intValue = 0)
    {
        if (!DebugTools.IsEnabled || string.IsNullOrEmpty(actionId))
            return false;

        foreach (var action in actions)
        {
            if (action.Id != actionId)
                continue;

            if (!action.IsAvailable(context))
            {
                Debug.LogWarning($"[DebugTools] Action unavailable: {action.Label}");
                return false;
            }

            action.Execute(context, intValue);
            return true;
        }

        Debug.LogWarning($"[DebugTools] Unknown action: {actionId}");
        return false;
    }

    public void ForceWin()
    {
        levelTarget?.ForceWin();
    }

    public void ForceLose()
    {
        levelTarget?.ForceLose();
    }

    public void RefreshPanel()
    {
        PanelRefreshRequested?.Invoke();
    }

    private void Register(IDebugAction action)
    {
        actions.Add(action);
    }

    private void ApplyStartingCoinsOverride()
    {
        var settings = DebugToolsSettings.Load();
        if (settings.startingCoinsOverride <= 0)
            return;

        if (settings.applyOnlyOnFreshSave)
        {
            var data = saveService.PlayerData;
            if (data.nextLevelIndex > 0 || data.coins > 0)
                return;
        }

        economyService.SetBalance(settings.startingCoinsOverride);
        Debug.Log($"[DebugTools] Applied starting coins override: {settings.startingCoinsOverride}");
    }

    public void Dispose()
    {
        levelTarget = null;
    }
}
