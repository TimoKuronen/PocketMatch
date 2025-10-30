using VContainer;
using VContainer.Unity;

public class BootstrapLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<ISaveService, SaveService>(Lifetime.Singleton);
        builder.Register<IAudioService, AudioService>(Lifetime.Singleton);
        builder.Register<IAdsService, AdsService>(Lifetime.Singleton);
        builder.Register<IAnalyticsService, AnalyticsService>(Lifetime.Singleton);
        builder.Register<IInputService, InputService>(Lifetime.Singleton);

        //builder.RegisterComponentInHierarchy<FirebaseInitializer>();
    }

    private new void Awake()
    {
        UnityEngine.Debug.Log("BootstrapLifetimeScope Awake");
        DontDestroyOnLoad(this.gameObject);
        base.Awake();
    }
}
