public class GameServices : Services
{
    protected override void Initialize()
    {
        var inputManager = new InputService();
        AddService<IInputService>(inputManager);

        var soundManager = new SoundManager();
        AddService<ISoundManager>(soundManager);

        var saveService = new SaveManager();
        AddService<ISaveService>(saveService);

        var GamesessionService = new GameSessionService();
        AddService<IGameSessionService>(GamesessionService);

        var levelManager = new LevelManager();
        AddService<ILevelManager>(levelManager);

        foreach (var service in serviceMap.Values)
        {
            service.Initialize();
        }
    }
}
