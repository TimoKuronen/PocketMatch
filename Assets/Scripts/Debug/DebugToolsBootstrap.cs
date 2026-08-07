using UnityEngine;
using VContainer;

public sealed class DebugToolsBootstrap : MonoBehaviour
{
    [Inject] private IDebugToolsService debugToolsService;
    [Inject] private ISaveService saveService;
    [Inject] private IEconomyService economyService;
    [Inject] private IAdsService adsService;

    private void Start()
    {
        if (!DebugTools.IsEnabled)
            return;

        var panelHost = new GameObject("DebugTools");
        panelHost.transform.SetParent(transform, false);

        var panel = panelHost.AddComponent<DebugToolsPanel>();
        panel.Initialize(debugToolsService, saveService, economyService, adsService);

        var hotkeys = panelHost.AddComponent<DebugHotkeyListener>();
        hotkeys.Initialize(debugToolsService);
    }
}
