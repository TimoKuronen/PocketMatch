using UnityEngine;
using VContainer;
using VContainer.Unity;

[DefaultExecutionOrder(100)]
public class MenuLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<MenuStackManager>(Lifetime.Scoped);
        
        builder.RegisterComponentInHierarchy<MainMenuPanel>()
               .As<IMainMenuView>();
        builder.RegisterComponentInHierarchy<MainMenuSettingsPanel>()
               .As<IMainMenuSettingsView>();
        builder.RegisterComponentInHierarchy<ConfirmationDialog>();
        builder.RegisterComponentInHierarchy<LevelSelectPanel>()
               .As<ILevelSelectView>();

        builder.Register<MainMenuPresenter>(Lifetime.Scoped).As<IStartable>();
        builder.Register<MainMenuSettingsPresenter>(Lifetime.Scoped).As<IStartable>();
        builder.Register<LevelSelectPresenter>(Lifetime.Scoped).As<IStartable>();
    }
}
