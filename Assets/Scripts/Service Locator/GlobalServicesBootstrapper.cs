
public class GlobalServicesBootstrapper : Services
{
    protected override void InitializeGlobalServices()
    {
        AddGlobalService<IAnalyticsManager>(new AnalyticsManager());
        AddGlobalService<ISoundManager>(new SoundManager());
        AddGlobalService<IAdsManager>(new AdsManager());
        AddGlobalService<ISettingsService>(new SettingsManager());

        InitializeAllGlobalServices();
    }
}
