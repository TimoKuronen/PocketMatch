using VContainer;
using VContainer.Unity;

public class MenuLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // Register MenuStackManager for main menu scene
        builder.Register<MenuStackManager>(Lifetime.Scoped);
        
        builder.RegisterComponentInHierarchy<MainMenuPanel>();
        builder.RegisterComponentInHierarchy<SettingsPanel>();
        builder.RegisterComponentInHierarchy<ConfirmationDialog>();
    }
}
