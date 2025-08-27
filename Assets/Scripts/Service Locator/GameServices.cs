public class GameServices : Services
{
    protected override void Initialize()
    {
        var saveManager = new SaveManager();
        AddService<ISaveService>(saveManager);

        var inputManager = new InputService();
        AddService<IInputService>(inputManager);

        var soundManager = new SoundManager();
        AddService<ISoundManager>(soundManager);

        var GameSessionService = new GameSessionService();
        AddService<IGameSessionService>(GameSessionService);

        var levelManager = new LevelManager();
        AddService<ILevelManager>(levelManager);

        foreach (var service in serviceMap.Values)
        {
            service.Initialize();
        }
    }
}
