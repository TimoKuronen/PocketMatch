using VContainer;
using VContainer.Unity;

public class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<IGameSessionService, GameSessionService>(Lifetime.Scoped);
        builder.Register<ILevelManager, LevelManager>(Lifetime.Scoped).As<IStartable>();
        builder.Register<IScoreService, ScoreService>(Lifetime.Scoped).As<IStartable>();
        builder.Register<IEffectService, EffectService>(Lifetime.Singleton).As<IStartable>();
        builder.Register<MenuStackManager>(Lifetime.Scoped);

        builder.RegisterComponentInHierarchy<UIGameHUD>()
               .As<IGameHudView>();
        builder.RegisterComponentInHierarchy<SettingsPanel>()
               .As<ISettingsView>();
        builder.RegisterComponentInHierarchy<WinPanel>()
               .As<IWinView>();
        builder.RegisterComponentInHierarchy<LosePanel>()
               .As<ILoseView>();
        builder.RegisterComponentInHierarchy<ConfirmationDialog>();

        builder.RegisterComponentInHierarchy<GridController>().As<IGridController>();
        builder.RegisterComponentInHierarchy<GridAudioPlayer>();
        
        builder.Register<GameHudPresenter>(Lifetime.Scoped).As<IStartable>();
        builder.Register<SettingsPresenter>(Lifetime.Scoped).As<IStartable>();
        builder.Register<WinPresenter>(Lifetime.Scoped).As<IStartable>();
        builder.Register<LosePresenter>(Lifetime.Scoped).As<IStartable>();

        builder.RegisterBuildCallback(container =>
        {
            var menuStackManager = container.Resolve<MenuStackManager>();
            var gridController = container.Resolve<IGridController>();
            menuStackManager.SetGridController(gridController);
        });
    }
}