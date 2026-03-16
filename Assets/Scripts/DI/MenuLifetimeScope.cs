using VContainer;
using VContainer.Unity;

public class MenuLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<MenuStackManager>(Lifetime.Scoped);
        
        builder.RegisterComponentInHierarchy<MainMenuPanel>()
               .As<IMainMenuView>();
        builder.RegisterComponentInHierarchy<SettingsPanel>()
               .As<ISettingsView>();
        builder.RegisterComponentInHierarchy<ConfirmationDialog>();

        builder.Register<MainMenuPresenter>(Lifetime.Scoped).As<IStartable>();
    }
}
