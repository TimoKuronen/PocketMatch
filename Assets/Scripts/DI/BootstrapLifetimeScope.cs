using VContainer;
using VContainer.Unity;

public class BootstrapLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<ISaveService, SaveService>(Lifetime.Singleton).
            As<IStartable>();
        builder.Register<IEconomyService, EconomyService>(Lifetime.Singleton).
            As<IStartable>();
        builder.Register<IAnalyticsService, AnalyticsService>(Lifetime.Singleton).
            As<IStartable>();
        builder.Register<IInputService, InputService>(Lifetime.Singleton).
            As<ITickable>();

        builder.Register<IAudioService, AudioService>(Lifetime.Singleton);
        builder.Register<IAdsService, AdsService>(Lifetime.Singleton);
        builder.Register<FirebaseInitializer>(Lifetime.Singleton);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        builder.Register<IDebugToolsService, DebugToolsService>(Lifetime.Singleton).
            As<IStartable>();
        builder.RegisterComponentInHierarchy<DebugToolsBootstrap>();
#else
        builder.Register<IDebugToolsService, NullDebugToolsService>(Lifetime.Singleton);
#endif
        builder.RegisterComponentInHierarchy<CloudSaveBootstrap>();
    }

    private new void Awake()
    {
        UnityEngine.Debug.Log("BootstrapLifetimeScope Awake");
        DontDestroyOnLoad(this.gameObject);
        base.Awake();
    }
}
