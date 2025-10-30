using VContainer;
using VContainer.Unity;

public class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        //AddSceneService<IInputService>(new InputService());
        //AddSceneService<ISaveService>(new SaveManager());
        //AddSceneService<IGameSessionService>(new GameSessionService());
        //AddSceneService<ILevelManager>(new LevelManager());
        //AddSceneService<IScoreManager>(new ScoreManager());
    }
}
