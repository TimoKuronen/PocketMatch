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

        var analyticsManager = new AnalyticsManager();
        AddService<IAnalyticsManager>(analyticsManager, isGlobal: true);

        foreach (var globalService in globalServices.Values)
        {
            globalService.Initialize();
        }

        foreach (var service in serviceMap.Values)
        {
            service.Initialize();
        }
    }
}