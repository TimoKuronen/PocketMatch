
public class GlobalServicesBootstrapper : Services
{
    protected override void InitializeGlobalServices()
    {
        AddGlobalService<IAnalyticsManager>(new AnalyticsManager());
        AddGlobalService<ISoundManager>(new SoundManager());

        InitializeAllGlobalServices();
    }
}
