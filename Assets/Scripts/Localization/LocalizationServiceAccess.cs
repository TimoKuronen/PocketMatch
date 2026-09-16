using VContainer;
using VContainer.Unity;

/// <summary>
/// Resolves the bootstrap localization singleton for MonoBehaviours outside a child LifetimeScope
/// (static TMP binders and loader chrome).
/// </summary>
public static class LocalizationServiceAccess
{
    public static bool TryGet(out ILocalizationService localization)
    {
        localization = null;

        var scope = LifetimeScope.Find<BootstrapLifetimeScope>();
        if (scope == null || scope.Container == null)
            return false;

        localization = scope.Container.Resolve<ILocalizationService>();
        return localization != null;
    }
}
