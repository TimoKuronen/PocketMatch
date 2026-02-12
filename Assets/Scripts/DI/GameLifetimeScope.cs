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
        
        // Register MenuStackManager as singleton
        builder.Register<MenuStackManager>(Lifetime.Scoped);

        builder.RegisterComponentInHierarchy<UI_GameHUD>();
        builder.RegisterComponentInHierarchy<UI_SettingsMenu>();
        builder.RegisterComponentInHierarchy<UI_WinPanel>();
        builder.RegisterComponentInHierarchy<UI_LosePanel>();
        builder.RegisterComponentInHierarchy<UI_ConfirmationDialog>();

        builder.RegisterComponentInHierarchy<GridController>()
            .As<IGridController>();
        builder.RegisterComponentInHierarchy<GridAudioPlayer>()
            .As<IStartable>();
    }
}