using VContainer;
using VContainer.Unity;

public class BootstrapLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<ISaveService, SaveService>(Lifetime.Singleton)
            .As<IStartable>();
        builder.Register<IAudioService, AudioService>(Lifetime.Singleton);
        builder.Register<IEffectService, EffectService>(Lifetime.Singleton);
        builder.Register<IAdsService, AdsService>(Lifetime.Singleton);
        builder.Register<IAnalyticsService, AnalyticsService>(Lifetime.Singleton)
            .As<IStartable>();
        builder.Register<IInputService, InputService>(Lifetime.Singleton);
        // ScoreService moved to GameLifetimeScope because it depends on IGridController

        builder.RegisterComponentInHierarchy<CloudSaveBootstrap>();

        builder.Register<FirebaseInitializer>(Lifetime.Singleton);
        builder.Register<GameSettingsService>(Lifetime.Singleton);
    }

    private new void Awake()
    {
        UnityEngine.Debug.Log("BootstrapLifetimeScope Awake");
        DontDestroyOnLoad(this.gameObject);
        base.Awake();
    }
}
