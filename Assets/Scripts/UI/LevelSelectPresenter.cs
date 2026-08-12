using Cysharp.Threading.Tasks;
using System;
using VContainer.Unity;

/// <summary>
/// Level-select MVP presenter; refreshes the grid when the view requests display, then loads play on selection.
/// </summary>
public class LevelSelectPresenter : IStartable, IDisposable
{
    private readonly ILevelSelectView view;
    private readonly ISaveService saveService;
    private readonly MenuStackManager menuStackManager;
    private readonly IAdsService adsService;

    public LevelSelectPresenter(
        ILevelSelectView view,
        ISaveService saveService,
        MenuStackManager menuStackManager,
        IAdsService adsService)
    {
        this.view = view;
        this.saveService = saveService;
        this.menuStackManager = menuStackManager;
        this.adsService = adsService;
    }

    public void Start()
    {
        view.BackClicked += OnBackClicked;
        view.LevelSelected += OnLevelSelected;
        view.DisplayRequested += OnDisplayRequested;
    }

    private void OnDisplayRequested()
    {
        RefreshLevelsAsync().Forget();
    }

    private async UniTaskVoid RefreshLevelsAsync()
    {
        int totalLevels = await LevelCatalog.GetTotalLevelsAsync();
        if (totalLevels <= 0)
        {
            UnityEngine.Debug.LogWarning("[LevelSelectPresenter] No levels found.");
            return;
        }

        int unlockedThroughIndex = saveService.PlayerData.nextLevelIndex;
        view.BindLevels(totalLevels, unlockedThroughIndex, unlockedThroughIndex);
    }

    private void OnBackClicked()
    {
        menuStackManager.PopMenu();
    }

    private void OnLevelSelected(int levelIndex)
    {
        if (levelIndex > saveService.PlayerData.nextLevelIndex)
            return;

        GameSignals.SetPendingLevelIndex(levelIndex);
        menuStackManager.ClearStack();
        adsService.HideBannerAd();
        Loader.Load(Loader.GameScene.PlayScene);
    }

    public void Dispose()
    {
        view.BackClicked -= OnBackClicked;
        view.LevelSelected -= OnLevelSelected;
        view.DisplayRequested -= OnDisplayRequested;
    }
}
