public class MenuServices : Services
{
    protected override void Initialize()
    {
        // Initialize all services
        foreach (var service in serviceMap.Values)
        {
            service.Initialize();
        }
    }
}
