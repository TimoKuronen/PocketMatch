public class GameServices : Services
{
    protected override void InitializeSceneServices()
    {
        AddSceneService<IInputService>(new InputService());
        AddSceneService<ISaveService>(new SaveManager());
        AddSceneService<IGameSessionService>(new GameSessionService());
        AddSceneService<ILevelManager>(new LevelManager());
        AddSceneService<IScoreManager>(new ScoreManager());

        InitializeAllSceneServices();
    }
}