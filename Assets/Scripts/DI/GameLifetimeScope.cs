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

        builder.RegisterComponentInHierarchy<UI_GameMenu>();

        builder.RegisterComponentInHierarchy<GridController>()
            .As<IGridController>();
        builder.RegisterComponentInHierarchy<GridAudioPlayer>()
            .As<IStartable>();
    }
}