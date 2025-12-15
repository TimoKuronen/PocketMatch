using UnityEngine;
using VContainer;
using VContainer.Unity;

public class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    { 
        builder.Register<IGameSessionService, GameSessionService>(Lifetime.Scoped);
        builder.Register<ILevelManager, LevelManager>(Lifetime.Scoped)
            .As<ITickable>();
        
        builder.Register<IScoreService, ScoreService>(Lifetime.Scoped);

        builder.RegisterComponentInHierarchy<UI_GameMenu>();
        builder.RegisterComponentInHierarchy<GridController>()
            .As<IGridController>();
    }
}