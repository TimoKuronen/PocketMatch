public class MenuServices : Services
{
    protected override void Initialize()
    {
        var inputManager = new InputService();
        AddService<IInputService>(inputManager);

        var soundManager = new SoundManager();
        AddService<ISoundManager>(soundManager);

        var saveService = new SaveManager();
        AddService<ISaveService>(saveService);

        foreach (var service in serviceMap.Values)
        {
            service.Initialize();
        }
    }
}