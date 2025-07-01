using UnityEngine;

public class GameServices : Services
{
    protected override void Initialize()
    {
        var inputManager = new InputService();
        AddService<IInputService>(inputManager);

        var soundManager = new SoundManager();
        AddService<ISoundManager>(soundManager);

        foreach (var service in serviceMap.Values)
        {
            service.Initialize();
        }
    }
}
