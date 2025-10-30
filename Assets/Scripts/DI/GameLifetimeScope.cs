using VContainer;
using VContainer.Unity;

public class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<IGameSessionService, GameSessionService>(Lifetime.Scoped);
        builder.Register<ILevelManager, LevelManager>(Lifetime.Scoped);

        builder.RegisterComponentInHierarchy<UI_GameMenu>();
    }
}
