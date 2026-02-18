using VContainer;
using VContainer.Unity;

public class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<IGameSessionService, GameSessionService>(Lifetime.Scoped);
        builder.Register<ILevelManager, LevelManager>(Lifetime.Scoped)
            .As<IStartable>();

        builder.Register<IScoreService, ScoreService>(Lifetime.Scoped)
            .As<IStartable>();
        builder.Register<IEffectService, EffectService>(Lifetime.Singleton).As<IStartable>();
        builder.Register<MenuStackManager>(Lifetime.Scoped);

        builder.RegisterComponentInHierarchy<UIGameHUD>();
        builder.RegisterComponentInHierarchy<SettingsPanel>();
        builder.RegisterComponentInHierarchy<WinPanel>();
        builder.RegisterComponentInHierarchy<LosePanel>();
        builder.RegisterComponentInHierarchy<ConfirmationDialog>();

        builder.RegisterComponentInHierarchy<GridController>()
            .As<IGridController>();
        builder.RegisterComponentInHierarchy<GridAudioPlayer>()
            .As<IStartable>();
        
        builder.RegisterBuildCallback(container =>
        {
            var menuStackManager = container.Resolve<MenuStackManager>();
            var gridController = container.Resolve<IGridController>();
            menuStackManager.SetGridController(gridController);
        });
    }
}